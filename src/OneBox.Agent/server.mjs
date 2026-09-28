import http from 'node:http';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { chromium } from 'playwright';

const PORT = Number(process.env.PORT || 8787);
const SESSION_SECRET = process.env.ONEBOX_AGENT_SECRET || '';
const OPENAI_KEY = process.env.OPENAI_API_KEY || '';
const OPENAI_BASE = process.env.OPENAI_BASE_URL || 'https://api.openai.com/v1/responses';
const OPENAI_MODEL = process.env.OPENAI_MODEL || 'gpt-5.6-luna';
const DATA_DIR = process.env.ONEBOX_BROWSER_DATA_DIR || path.resolve('./browser-data');
fs.mkdirSync(DATA_DIR, { recursive: true });
const WEB_ORIGIN = process.env.ONEBOX_WEB_ORIGIN || 'http://localhost:5173';
const allowedHosts = (process.env.ONEBOX_ALLOWED_HOSTS || '').split(',').map(x=>x.trim().toLowerCase()).filter(Boolean);
const context = await chromium.launchPersistentContext(DATA_DIR,{headless:true,acceptDownloads:true});
let page = context.pages()[0] || await context.newPage();

function ok(res,data){res.writeHead(200,{'content-type':'application/json','access-control-allow-origin':WEB_ORIGIN,'access-control-allow-methods':'GET,POST,OPTIONS','access-control-allow-headers':'content-type,x-onebox-session'});res.end(JSON.stringify(data));}
function fail(res,code,message){res.writeHead(code,{'content-type':'application/json','access-control-allow-origin':WEB_ORIGIN,'access-control-allow-methods':'GET,POST,OPTIONS','access-control-allow-headers':'content-type,x-onebox-session'});res.end(JSON.stringify({success:false,status:'ERROR',message}));}
function authorized(req){ if(!SESSION_SECRET) return true; const supplied=String(req.headers['x-onebox-session']||''); const parts=supplied.split('.'); if(parts.length!==3)return false; const [exp,nonce,sig]=parts; if(Number(exp)<Math.floor(Date.now()/1000))return false; const expected=crypto.createHmac('sha256',SESSION_SECRET).update(`${exp}.${nonce}`).digest('hex'); return sig.length===expected.length && crypto.timingSafeEqual(Buffer.from(sig),Buffer.from(expected)); }
function hostAllowed(url){ if(!allowedHosts.length)return true; try{const h=new URL(url).hostname.toLowerCase();return allowedHosts.some(x=>h===x||h.endsWith('.'+x));}catch{return false;} }
function simplifyHtml(html){return html.replace(/<script[\s\S]*?<\/script>/gi,'').replace(/<style[\s\S]*?<\/style>/gi,'').replace(/\s+/g,' ').slice(0,100000)}
async function askModel(goal,html){
  if(!OPENAI_KEY) throw new Error('OPENAI_API_KEY is required for browser-agent planning.');
  const schema={type:'json_schema',name:'browser_plan',strict:true,schema:{type:'object',properties:{actions:{type:'array',items:{type:'object',properties:{kind:{type:'string',enum:['click','fill','select','press','wait','extract']},selector:{type:'string'},value:{type:'string'}},required:['kind','selector','value'],additionalProperties:false}},stopsBeforeIrreversibleAction:{type:'boolean'},reason:{type:'string'}},required:['actions','stopsBeforeIrreversibleAction','reason'],additionalProperties:false}};
  const body={model:OPENAI_MODEL,store:false,instructions:'You are the ONEBOX browser planner. Use only the supplied DOM. Produce deterministic Playwright-compatible actions. Never use arbitrary JavaScript. Do not bypass CAPTCHA, MFA, OTP, biometric checks, paywalls, access controls, or anti-bot mechanisms. Never invent selectors or values. You may navigate and fill reversible fields. Stop at the final irreversible payment, purchase, booking, cancellation, or submission control and let the user confirm it in the provider UI.',input:`GOAL:\n${goal}\n\nDOM:\n${html}`,text:{format:schema}};
  const r=await fetch(OPENAI_BASE,{method:'POST',headers:{authorization:`Bearer ${OPENAI_KEY}`,'content-type':'application/json'},body:JSON.stringify(body)});
  if(!r.ok) throw new Error(`Planner API failed: ${r.status}`);
  const j=await r.json(); const out=j.output_text || j.output?.flatMap(x=>x.content||[]).find(x=>x.text)?.text || '{}'; return JSON.parse(out);
}
async function prepare(goal,startUrl){
  if(!hostAllowed(startUrl))throw new Error('Provider domain is not allowed. Add the provider host to ONEBOX_ALLOWED_HOSTS.');
  await page.goto(startUrl,{waitUntil:'domcontentloaded',timeout:30000});
  const html=simplifyHtml(await page.content());
  const plan=await askModel(goal,html);
  return {success:true,status:'READY_FOR_CONFIRMATION',message:'Provider page opened. ONEBOX prepared reversible actions and stopped before the final irreversible step.',providerUrl:page.url(),plan};
}
function irreversible(a){const s=((a.selector||'')+' '+(a.value||'')).toLowerCase();return /\b(pay|purchase|buy|book|reserve|confirm|submit|place order|cancel|delete|transfer|send money|checkout)\b/.test(s)}
async function execute(plan,finalConfirm){
  if(!finalConfirm)throw new Error('Final confirmation is required.');
  if(plan.stopsBeforeIrreversibleAction!==true)throw new Error('Execution plan did not prove that it stops before the irreversible action.');
  for(const a of (plan.actions||[])){
    if(irreversible(a))throw new Error('ONEBOX blocked an irreversible browser action. The provider confirmation/payment step must remain under explicit user control.');
    const timeout=Math.min(Number(a.timeoutMs||15000),30000);
    if(a.kind==='wait')await page.waitForTimeout(Math.min(Number(a.value||1000),10000));
    else if(a.kind==='click')await page.locator(a.selector).first().click({timeout});
    else if(a.kind==='fill')await page.locator(a.selector).first().fill(a.value||'',{timeout});
    else if(a.kind==='select')await page.locator(a.selector).first().selectOption(a.value||'',{timeout});
    else if(a.kind==='press')await page.locator(a.selector).first().press(a.value||'Enter',{timeout});
    else if(a.kind==='extract'){if(!plan.data)plan.data={};plan.data[a.value||'value']=await page.locator(a.selector).first().innerText({timeout});}
    else throw new Error(`Unsupported action ${a.kind}`);
  }
  return {success:true,status:'EXECUTED',message:'Provider actions executed. Any OTP, CAPTCHA, UPI PIN, biometric or other user-authentication step remains in the provider UI.',providerReference:null,providerUrl:page.url(),data:plan.data||{}};
}
const server=http.createServer((req,res)=>{
  if(req.method==='OPTIONS'){res.writeHead(204,{'access-control-allow-origin':WEB_ORIGIN,'access-control-allow-methods':'GET,POST,OPTIONS','access-control-allow-headers':'content-type,x-onebox-session'});return res.end();}
  if(req.method==='GET'&&req.url==='/health')return ok(res,{status:'ok',service:'onebox-local-agent',browserDataDir:DATA_DIR});
  if(!authorized(req))return fail(res,401,'Invalid execution session.');
  if(req.method!=='POST'||!['/prepare','/execute'].includes(req.url))return fail(res,404,'Not found');
  let body='';req.on('data',c=>body+=c);req.on('end',async()=>{try{const p=JSON.parse(body);if(req.url==='/prepare'){if(!p.goal||!p.startUrl)throw new Error('goal and startUrl are required');return ok(res,await prepare(p.goal,p.startUrl));}if(req.url==='/execute'){if(!p.plan)throw new Error('plan is required');return ok(res,await execute(p.plan,p.finalConfirm===true));}}catch(e){fail(res,400,e.message)}});
});
server.listen(PORT,'0.0.0.0',()=>console.log(`ONEBOX local agent listening on http://0.0.0.0:${PORT}`));
