namespace OneBox.Api.Services;
public sealed record LanguageInfo(string Code,string Name,string NativeName,string Direction);
public static class LanguageService
{
    private static readonly LanguageInfo[] Supported =
    [
        new("en","English","English","ltr"),new("kn","Kannada","ಕನ್ನಡ","ltr"),new("te","Telugu","తెలుగు","ltr"),
        new("ta","Tamil","தமிழ்","ltr"),new("ml","Malayalam","മലയാളം","ltr"),new("hi","Hindi","हिन्दी","ltr"),
        new("mr","Marathi","मराठी","ltr"),new("ur","Urdu","اردو","rtl"),new("pa","Punjabi","ਪੰਜਾਬੀ","ltr"),
        new("fr","French","Français","ltr"),new("pt","Portuguese","Português","ltr")
    ];
    public static IReadOnlyList<LanguageInfo> All=>Supported;
    public static string Detect(string text)
    {
        if(string.IsNullOrWhiteSpace(text))return "en";
        if(text.Any(c=>c>='\u0C80'&&c<='\u0CFF'))return "kn";
        if(text.Any(c=>c>='\u0C00'&&c<='\u0C7F'))return "te";
        if(text.Any(c=>c>='\u0B80'&&c<='\u0BFF'))return "ta";
        if(text.Any(c=>c>='\u0D00'&&c<='\u0D7F'))return "ml";
        if(text.Any(c=>c>='\u0900'&&c<='\u097F')){if(text.Contains("आहे")||text.Contains("मला")||text.Contains("करा"))return "mr";return "hi";}
        if(text.Any(c=>c>='\u0600'&&c<='\u06FF'))return "ur";
        if(text.Any(c=>c>='\u0A00'&&c<='\u0A7F'))return "pa";
        return "en";
    }
    public static LanguageInfo Get(string code)=>Supported.FirstOrDefault(x=>x.Code==code)??Supported[0];
    public static string SameLanguageInstruction(string language)=>$"The user's preferred language is {Get(language).Name} (code {language}). Respond to the user in that language whenever practical. Preserve product names, URLs, amounts, dates, provider names and structured task fields exactly.";
    public static string Text(string key,string language)=>(key,language) switch
    {
        ("confirm","kn")=>"ನಾನು ಈ ಕಾರ್ಯವನ್ನು ಸಿದ್ಧಪಡಿಸಿದ್ದೇನೆ. ವಿವರಗಳನ್ನು ಪರಿಶೀಲಿಸಿ ಮತ್ತು ಖಚಿತಪಡಿಸಿ. ಯಾವುದೇ ಅಂತಿಮ ಕ್ರಮವನ್ನು ಇನ್ನೂ ಮಾಡಲಾಗಿಲ್ಲ.",
        ("confirm","te")=>"నేను ఈ పనిని సిద్ధం చేశాను. వివరాలను పరిశీలించి నిర్ధారించండి. ఇంకా తుది చర్య ಮಾಡಿಲ್ಲ.",
        ("confirm","ta")=>"இந்த பணியை தயார் செய்துள்ளேன். விவரங்களை சரிபார்த்து உறுதிப்படுத்துங்கள். இறுதி நடவடிக்கை இன்னும் செய்யப்படவில்லை.",
        ("confirm","ml")=>"ഈ പ്രവർത്തനം ഞാൻ തയ്യാറാക്കി. വിവരങ്ങൾ പരിശോധിച്ച് സ്ഥിരീകരിക്കുക. അന്തിമ നടപടി ഇതുവരെ നടത്തിയിട്ടില്ല.",
        ("confirm","hi")=>"मैंने यह कार्य तैयार कर दिया है। विवरण जाँचकर पुष्टि करें। अभी कोई अंतिम कार्रवाई नहीं की गई है।",
        ("confirm","mr")=>"मी हे काम तयार केले आहे. तपशील तपासून पुष्टी करा. अंतिम कृती अजून केलेली नाही.",
        ("confirm","ur")=>"میں نے یہ کام تیار کر دیا ہے۔ تفصیلات چیک کرکے تصدیق کریں۔ ابھی کوئی حتمی کارروائی نہیں کی گئی۔",
        ("confirm","pa")=>"ਮੈਂ ਇਹ ਕੰਮ ਤਿਆਰ ਕਰ ਦਿੱਤਾ ਹੈ। ਵੇਰਵੇ ਜਾਂਚ ਕੇ ਪੁਸ਼ਟੀ ਕਰੋ। ਹਾਲੇ ਕੋਈ ਅੰਤਿਮ ਕਾਰਵਾਈ ਨਹੀਂ ਕੀਤੀ।",
        ("confirm","fr")=>"J’ai préparé cette tâche. Vérifiez les détails puis confirmez. Aucune action irréversible n’a encore été effectuée.",
        ("confirm","pt")=>"Preparei esta tarefa. Verifique os detalhes e confirme. Nenhuma ação irreversível foi realizada.",
        ("confirm",_)=>"I prepared the task. Review the exact provider, amount and action, then confirm. No irreversible action has happened.",
        ("location","kn")=>"ಹತ್ತಿರದ ಸೇವೆಗಳನ್ನು ಹುಡುಕಲು ನಿಮ್ಮ ಸ್ಥಳದ ಅನುಮತಿ ನೀಡಿ.",
        ("location","te")=>"సమీపంలోని సేవలను కనుగొనడానికి మీ లొకేషన్ అనుమతించండి.",
        ("location","ta")=>"அருகிலுள்ள சேவைகளைத் தேட உங்கள் இருப்பிட அனுமதியை வழங்குங்கள்.",
        ("location","ml")=>"സമീപ സേവനങ്ങൾ കണ്ടെത്താൻ നിങ്ങളുടെ ലൊക്കേഷൻ അനുമതി നൽകുക.",
        ("location","hi")=>"पास की सेवाएँ खोजने के लिए अपनी लोकेशन की अनुमति दें।",
        ("location","mr")=>"जवळच्या सेवा शोधण्यासाठी तुमचे लोकेशन वापरण्याची परवानगी द्या.",
        ("location","ur")=>"قریبی خدمات تلاش کرنے کے لیے اپنی لوکیشن کی اجازت دیں۔",
        ("location","pa")=>"ਨੇੜਲੀਆਂ ਸੇਵਾਵਾਂ ਲੱਭਣ ਲਈ ਆਪਣੀ ਲੋਕੇਸ਼ਨ ਦੀ ਇਜਾਜ਼ਤ ਦਿਓ।",
        ("location","fr")=>"Autorisez votre localisation pour rechercher les services à proximité.",
        ("location","pt")=>"Permita o acesso à localização para encontrar serviços próximos.",
        ("location",_)=>"Allow location access before I search nearby providers.",
        _=>key
    };
}