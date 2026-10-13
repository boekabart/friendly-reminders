global using static Loc;
using System.Globalization;

/// <summary>
/// All visible texts. Every property is <c>required</c>: a missing translation is a compile error.
/// Formatting via <see cref="Loc.F"/>: {n:t} = short time in the chosen culture (en-US "2:03 PM", en-GB/nl/sv "14:03").
/// </summary>
sealed record Texts
{
    // Tray menu
    public required string MenuUpcoming { get; init; }
    public required string MenuRefresh { get; init; }
    public required string MenuTestPopup { get; init; }
    public required string MenuChatPopup { get; init; }
    public required string MenuAutostart { get; init; }
    public required string MenuSettings { get; init; }            // {0} where: "OneDrive" or "this PC"
    public required string ThisPc { get; init; }
    public required string MenuExit { get; init; }
    public required string MenuSignInTo { get; init; }           // {0} name
    public required string MenuGrantAccess { get; init; }        // {0} name, {1} features with permission
    public required string MenuUpdate { get; init; }             // {0} new version

    // Status lines in the menu: {0} name, {1} time
    public required string StatusNotFetched { get; init; }
    public required string StatusNoIcsLink { get; init; }
    public required string StatusCalendarOk { get; init; }       // {2} number of meetings
    public required string StatusOk { get; init; }
    public required string StatusError { get; init; }            // {2} message, {3} failures in a row
    public required string StatusSignInRequired { get; init; }
    public required string StatusSignedIn { get; init; }
    public required string StatusAccessNeeded { get; init; }     // {0} name, {1} permission (e.g. Calendars.Read)

    // Balloon notifications
    public required string BalloonConfigError { get; init; }     // {0} message
    public required string BalloonReloaded { get; init; }
    public required string BalloonConfigMoved { get; init; }      // {0} new path, {1} local folder
    public required string BalloonConfigOneDriveWins { get; init; }  // {0} OneDrive path, {1} local folder
    public required string BalloonCalendarFailed { get; init; }  // {0} name, {1} message
    public required string BalloonChatsFailed { get; init; }     // {0} name, {1} message
    public required string BalloonSignInAgain { get; init; }     // {0} name
    public required string BalloonSignInFailed { get; init; }    // {0} name, {1} message
    public required string BalloonAccessNeeded { get; init; }    // {0} name, {1} feature with permission
    public required string BalloonUpdateReady { get; init; }     // {0} new version
    public required string FeatureCalendar { get; init; }
    public required string FeatureChats { get; init; }

    // Windows and popups
    public required string UpcomingTitle { get; init; }
    public required string NoMeetings { get; init; }
    public required string ExampleMeeting { get; init; }
    public required string ExampleColleague { get; init; }
    public required string Join { get; init; }
    public required string JoinOn { get; init; }                 // {0} provider (Meet, Zoom)
    public required string Later { get; init; }
    public required string CloseButton { get; init; }
    public required string StartsIn { get; init; }               // {0} min, {1} sec, {2} start time
    public required string StartedNow { get; init; }
    public required string StartedAgo { get; init; }             // {0} min
    public required string ChatHeader { get; init; }             // {0} count
    public required string MoreChats { get; init; }              // {0} count
    public required string MarkAllSeen { get; init; }
    public required string SeenTooltip { get; init; }
    public required string MuteButton { get; init; }
    public required string SnoozeButton { get; init; }
    public required string QuietCurrentMeeting { get; init; }
    public required string QuietUntilEnd { get; init; }                           // {0} title, {1} end time
    public required string QuietMinutes { get; init; }                            // {0} minutes
    public required string QuietHour { get; init; }
    public required string QuietWhileScreens { get; init; }
    public required string QuietAlwaysSetup { get; init; }
    public required string MeetingsMuted { get; init; }
    public required string ChatsMuted { get; init; }
    public required string ChatsSnoozed { get; init; }
    public required string QuietUntil { get; init; }                              // {0} what, {1} time
    public required string QuietWhile { get; init; }                              // {0} what
    public required string QuietRemembered { get; init; }                         // {0} what
    public required string MoveTo { get; init; }                                  // {0} screen
    public required string MoveToScreen { get; init; }
    public required string NoOtherScreen { get; init; }
    public required string BuiltInDisplay { get; init; }
    public required string PrimaryName { get; init; }                             // {0} screen
    public required string MenuScreenSetups { get; init; }
    public required string ScreenSetupNow { get; init; }                          // {0} setup
    public required string NothingRemembered { get; init; }
    public required string SetupHeader { get; init; }                             // {0} setup, {1} last seen
    public required string SetupOn { get; init; }                                 // {0} what, {1} screen
    public required string WhatMeetings { get; init; }
    public required string WhatChats { get; init; }
    public required string Forget { get; init; }                                  // {0} choice
    public required string ForgetSetup { get; init; }
    public required string ForgetAll { get; init; }
    public required string IgnoreTitle { get; init; }              // {0} title

    // Startup
    public required string AlreadyRunning { get; init; }
    public required string AutostartQuestion { get; init; }
    public required string ConfigCreated { get; init; }          // {0} path
    public required string ConfigErrorAtStart { get; init; }     // {0} path, {1} message

    // Data
    public required string NoTitle { get; init; }
    public required string UnknownChat { get; init; }
    public required string TooManyRequests { get; init; }        // {0} time
}

static class Loc
{
    static readonly Texts EnUs = new()
    {
        MenuUpcoming = "Upcoming meetings…",
        MenuRefresh = "Refresh now",
        MenuTestPopup = "Show test popup",
        MenuChatPopup = "Show chat popup",
        MenuAutostart = "Start with Windows",
        MenuSettings = "Open settings ({0})",
        ThisPc = "this PC",
        MenuExit = "Exit",
        MenuSignInTo = "Sign in to {0}…",
        MenuGrantAccess = "{0}: allow access to {1}…",
        MenuUpdate = "Restart to update to {0}",
        StatusNotFetched = "{0}: not fetched yet",
        StatusNoIcsLink = "{0}: no ICS link filled in yet",
        StatusCalendarOk = "{0}: OK at {1:t}, {2} upcoming meeting(s)",
        StatusOk = "{0}: OK at {1:t}",
        StatusError = "{0}: ERROR at {1:t} ({3}× in a row) - {2}",
        StatusSignInRequired = "{0}: sign-in required",
        StatusSignedIn = "{0}: signed in, fetching…",
        StatusAccessNeeded = "{0}: permission needed ({1})",
        BalloonConfigError = "Error in config.json, keeping the previous settings:\n{0}",
        BalloonReloaded = "Settings reloaded.",
        BalloonConfigMoved = "Settings moved to OneDrive, so they sync to your other PCs:\n{0}\nThe old file is config.moved.json in {1}.",
        BalloonConfigOneDriveWins = "Using the settings from OneDrive:\n{0}\nThis PC's own settings were kept as config.local-backup.json in {1}.",
        BalloonCalendarFailed = "Fetching calendar '{0}' failed:\n{1}",
        BalloonChatsFailed = "Fetching Teams chats '{0}' failed:\n{1}",
        BalloonSignInAgain = "{0}: please sign in (via the tray icon's menu).",
        BalloonSignInFailed = "Signing in to '{0}' failed:\n{1}",
        BalloonAccessNeeded = "{0}: Meeting Alarm needs access to {1}. Allow it via the tray icon's menu.",
        BalloonUpdateReady = "Version {0} is ready. It's installed the next time the app starts, or right away via the tray icon's menu.",
        FeatureCalendar = "your calendar",
        FeatureChats = "your Teams chats",
        UpcomingTitle = "Upcoming meetings",
        NoMeetings = "No meetings found in the next 2 days.",
        ExampleMeeting = "Example: this is what a meeting alert looks like",
        ExampleColleague = "Example Colleague",
        Join = "Join",
        JoinOn = "Join on {0}",
        Later = "Later",
        CloseButton = "Close",
        StartsIn = "Starts in {0}:{1:00}   ({2:t})",
        StartedNow = "STARTED NOW!",
        StartedAgo = "Started {0} min ago",
        ChatHeader = "Teams — {0} unread",
        MoreChats = "+ {0} more chats",
        MarkAllSeen = "Mark all as seen",
        SeenTooltip = "Seen (until new messages arrive)",
        MuteButton = "Mute",
        SnoozeButton = "Snooze",
        QuietCurrentMeeting = "Current meeting",
        QuietUntilEnd = "{0} (until {1:t})",
        QuietMinutes = "{0} minutes",
        QuietHour = "1 hour",
        QuietWhileScreens = "While connected to these screens",
        QuietAlwaysSetup = "Always in this screen setup",
        MeetingsMuted = "Meetings muted",
        ChatsMuted = "Chats muted",
        ChatsSnoozed = "Chats snoozed",
        QuietUntil = "{0} until {1:t} — click to resume",
        QuietWhile = "{0} while on these screens — click to resume",
        QuietRemembered = "{0} in this screen setup — click to forget",
        MoveTo = "Move to {0}",
        MoveToScreen = "Move to screen",
        NoOtherScreen = "No other screen",
        BuiltInDisplay = "Built-in display",
        PrimaryName = "{0} (primary)",
        MenuScreenSetups = "Screen setups",
        ScreenSetupNow = "Now: {0}",
        NothingRemembered = "Nothing remembered yet",
        SetupHeader = "{0} — last seen {1:g}",
        SetupOn = "{0} on {1}",
        WhatMeetings = "Meetings",
        WhatChats = "Chats",
        Forget = "Forget: {0}",
        ForgetSetup = "Forget this setup",
        ForgetAll = "Forget all",
        IgnoreTitle = "Ignore all meetings named “{0}”",
        AlreadyRunning = "Meeting Alarm is already running (see the system tray).",
        AutostartQuestion = "Start Meeting Alarm automatically with Windows?",
        ConfigCreated = "Settings file created:\n{0}\n\nFill in each job's Tenant (e.g. company.com) and LoginHint (your work email). " +
                        "Calendars from elsewhere can be added as ICS links. Save; changes are loaded automatically.",
        ConfigErrorAtStart = "Error in {0}:\n\n{1}",
        NoTitle = "(no title)",
        UnknownChat = "(chat)",
        TooManyRequests = "too many requests, pausing until {0:T}",
    };

    // British English: almost the same texts; the real difference is the culture's time format (14:03 instead of 2:03 PM).
    static readonly Texts EnGb = EnUs with
    {
        ConfigCreated = "Settings file created:\n{0}\n\nFill in each job's Tenant (e.g. company.co.uk) and LoginHint (your work e-mail). " +
                        "Calendars from elsewhere can be added as ICS links. Save; changes are loaded automatically.",
        AlreadyRunning = "Meeting Alarm is already running (see the notification area).",
    };

    static readonly Texts NlNl = new()
    {
        MenuUpcoming = "Komende meetings…",
        MenuRefresh = "Nu verversen",
        MenuTestPopup = "Test-popup tonen",
        MenuChatPopup = "Chat-popup tonen",
        MenuAutostart = "Start met Windows",
        MenuSettings = "Instellingen openen ({0})",
        ThisPc = "deze pc",
        MenuExit = "Afsluiten",
        MenuSignInTo = "Inloggen bij {0}…",
        MenuGrantAccess = "{0}: toegang geven tot {1}…",
        MenuUpdate = "Herstarten en bijwerken naar {0}",
        StatusNotFetched = "{0}: nog niet opgehaald",
        StatusNoIcsLink = "{0}: nog geen ICS-link ingevuld",
        StatusCalendarOk = "{0}: OK om {1:t}, {2} komende meeting(s)",
        StatusOk = "{0}: OK om {1:t}",
        StatusError = "{0}: FOUT om {1:t} ({3}× achter elkaar) - {2}",
        StatusSignInRequired = "{0}: inloggen vereist",
        StatusSignedIn = "{0}: ingelogd, ophalen…",
        StatusAccessNeeded = "{0}: toestemming nodig ({1})",
        BalloonConfigError = "Fout in config.json, oude instellingen blijven actief:\n{0}",
        BalloonReloaded = "Instellingen opnieuw geladen.",
        BalloonConfigMoved = "Instellingen verplaatst naar OneDrive, zodat ze naar je andere pc's synchroniseren:\n{0}\nHet oude bestand is config.moved.json in {1}.",
        BalloonConfigOneDriveWins = "De instellingen uit OneDrive worden gebruikt:\n{0}\nDe eigen instellingen van deze pc staan nog als config.local-backup.json in {1}.",
        BalloonCalendarFailed = "Agenda '{0}' ophalen mislukt:\n{1}",
        BalloonChatsFailed = "Teams-chats '{0}' ophalen mislukt:\n{1}",
        BalloonSignInAgain = "{0}: inloggen vereist (via het menu van het icoon).",
        BalloonSignInFailed = "Inloggen bij '{0}' mislukt:\n{1}",
        BalloonAccessNeeded = "{0}: Meeting Alarm heeft toegang nodig tot {1}. Geef die via het menu van het icoon.",
        BalloonUpdateReady = "Versie {0} staat klaar. Die wordt geïnstalleerd bij de volgende start van de app, of meteen via het menu van het icoon.",
        FeatureCalendar = "je agenda",
        FeatureChats = "je Teams-chats",
        UpcomingTitle = "Komende meetings",
        NoMeetings = "Geen meetings gevonden in de komende 2 dagen.",
        ExampleMeeting = "Voorbeeld: zo ziet een meeting-melding eruit",
        ExampleColleague = "Voorbeeld Collega",
        Join = "Deelnemen",
        JoinOn = "Deelnemen via {0}",
        Later = "Straks",
        CloseButton = "Sluiten",
        StartsIn = "Start over {0}:{1:00}   ({2:t})",
        StartedNow = "NU BEGONNEN!",
        StartedAgo = "Begonnen, {0} min geleden",
        ChatHeader = "Teams — {0} ongelezen",
        MoreChats = "+ {0} andere chats",
        MarkAllSeen = "Alles gezien",
        SeenTooltip = "Gezien (tot er nieuwe berichten komen)",
        MuteButton = "Dempen",
        SnoozeButton = "Sluimeren",
        QuietCurrentMeeting = "Huidige vergadering",
        QuietUntilEnd = "{0} (tot {1:t})",
        QuietMinutes = "{0} minuten",
        QuietHour = "1 uur",
        QuietWhileScreens = "Zolang deze schermen aangesloten zijn",
        QuietAlwaysSetup = "Altijd in deze schermopstelling",
        MeetingsMuted = "Vergaderingen gedempt",
        ChatsMuted = "Chats gedempt",
        ChatsSnoozed = "Chats sluimeren",
        QuietUntil = "{0} tot {1:t} — klik om te hervatten",
        QuietWhile = "{0} zolang deze schermen aangesloten zijn — klik om te hervatten",
        QuietRemembered = "{0} in deze schermopstelling — klik om te vergeten",
        MoveTo = "Verplaatsen naar {0}",
        MoveToScreen = "Verplaatsen naar scherm",
        NoOtherScreen = "Geen ander scherm",
        BuiltInDisplay = "Ingebouwd scherm",
        PrimaryName = "{0} (primair)",
        MenuScreenSetups = "Schermopstellingen",
        ScreenSetupNow = "Nu: {0}",
        NothingRemembered = "Nog niets onthouden",
        SetupHeader = "{0} — laatst gezien {1:g}",
        SetupOn = "{0} op {1}",
        WhatMeetings = "Vergaderingen",
        WhatChats = "Chats",
        Forget = "Vergeten: {0}",
        ForgetSetup = "Deze opstelling vergeten",
        ForgetAll = "Alles vergeten",
        IgnoreTitle = "Alle vergaderingen met de naam “{0}” negeren",
        AlreadyRunning = "Meeting Alarm draait al (zie systeemvak).",
        AutostartQuestion = "Meeting Alarm automatisch starten met Windows?",
        ConfigCreated = "Instellingenbestand aangemaakt:\n{0}\n\nVul per job de Tenant (bv. bedrijf.nl) en LoginHint (je werk-e-mail) in. " +
                        "Agenda's van elders kun je als ICS-link toevoegen. Sla op; wijzigingen worden automatisch geladen.",
        ConfigErrorAtStart = "Fout in {0}:\n\n{1}",
        NoTitle = "(geen titel)",
        UnknownChat = "(chat)",
        TooManyRequests = "te veel verzoeken, pauze tot {0:T}",
    };

    static readonly Texts SvSe = new()
    {
        MenuUpcoming = "Kommande möten…",
        MenuRefresh = "Uppdatera nu",
        MenuTestPopup = "Visa testpopup",
        MenuChatPopup = "Visa chattpopup",
        MenuAutostart = "Starta med Windows",
        MenuSettings = "Öppna inställningar ({0})",
        ThisPc = "den här datorn",
        MenuExit = "Avsluta",
        MenuSignInTo = "Logga in på {0}…",
        MenuGrantAccess = "{0}: ge åtkomst till {1}…",
        MenuUpdate = "Starta om och uppdatera till {0}",
        StatusNotFetched = "{0}: inte hämtad än",
        StatusNoIcsLink = "{0}: ingen ICS-länk angiven än",
        StatusCalendarOk = "{0}: OK kl. {1:t}, {2} kommande möte(n)",
        StatusOk = "{0}: OK kl. {1:t}",
        StatusError = "{0}: FEL kl. {1:t} ({3} gånger i rad) - {2}",
        StatusSignInRequired = "{0}: inloggning krävs",
        StatusSignedIn = "{0}: inloggad, hämtar…",
        StatusAccessNeeded = "{0}: behörighet krävs ({1})",
        BalloonConfigError = "Fel i config.json, de tidigare inställningarna behålls:\n{0}",
        BalloonReloaded = "Inställningarna har lästs in igen.",
        BalloonConfigMoved = "Inställningarna har flyttats till OneDrive och synkas till dina andra datorer:\n{0}\nDen gamla filen är config.moved.json i {1}.",
        BalloonConfigOneDriveWins = "Inställningarna från OneDrive används:\n{0}\nDen här datorns egna inställningar finns kvar som config.local-backup.json i {1}.",
        BalloonCalendarFailed = "Det gick inte att hämta kalendern '{0}':\n{1}",
        BalloonChatsFailed = "Det gick inte att hämta Teams-chattarna '{0}':\n{1}",
        BalloonSignInAgain = "{0}: logga in (via ikonens meny).",
        BalloonSignInFailed = "Inloggningen på '{0}' misslyckades:\n{1}",
        BalloonAccessNeeded = "{0}: Meeting Alarm behöver åtkomst till {1}. Tillåt det via ikonens meny.",
        BalloonUpdateReady = "Version {0} är klar. Den installeras nästa gång appen startar, eller direkt via ikonens meny.",
        FeatureCalendar = "din kalender",
        FeatureChats = "dina Teams-chattar",
        UpcomingTitle = "Kommande möten",
        NoMeetings = "Inga möten hittades de kommande 2 dagarna.",
        ExampleMeeting = "Exempel: så här ser en mötesavisering ut",
        ExampleColleague = "Exempel Kollega",
        Join = "Anslut",
        JoinOn = "Anslut via {0}",
        Later = "Senare",
        CloseButton = "Stäng",
        StartsIn = "Börjar om {0}:{1:00}   ({2:t})",
        StartedNow = "BÖRJAR NU!",
        StartedAgo = "Började för {0} min sedan",
        ChatHeader = "Teams — {0} olästa",
        MoreChats = "+ {0} chattar till",
        MarkAllSeen = "Markera alla som sedda",
        SeenTooltip = "Sedd (tills nya meddelanden kommer)",
        MuteButton = "Tysta",
        SnoozeButton = "Snooza",
        QuietCurrentMeeting = "Pågående möte",
        QuietUntilEnd = "{0} (till {1:t})",
        QuietMinutes = "{0} minuter",
        QuietHour = "1 timme",
        QuietWhileScreens = "Så länge de här skärmarna är anslutna",
        QuietAlwaysSetup = "Alltid i den här skärmuppsättningen",
        MeetingsMuted = "Möten tystade",
        ChatsMuted = "Chattar tystade",
        ChatsSnoozed = "Chattar snoozade",
        QuietUntil = "{0} till {1:t} — klicka för att återuppta",
        QuietWhile = "{0} så länge de här skärmarna är anslutna — klicka för att återuppta",
        QuietRemembered = "{0} i den här skärmuppsättningen — klicka för att glömma",
        MoveTo = "Flytta till {0}",
        MoveToScreen = "Flytta till skärm",
        NoOtherScreen = "Ingen annan skärm",
        BuiltInDisplay = "Inbyggd skärm",
        PrimaryName = "{0} (primär)",
        MenuScreenSetups = "Skärmuppsättningar",
        ScreenSetupNow = "Nu: {0}",
        NothingRemembered = "Inget sparat än",
        SetupHeader = "{0} — senast sedd {1:g}",
        SetupOn = "{0} på {1}",
        WhatMeetings = "Möten",
        WhatChats = "Chattar",
        Forget = "Glöm: {0}",
        ForgetSetup = "Glöm den här uppsättningen",
        ForgetAll = "Glöm alla",
        IgnoreTitle = "Ignorera alla möten med namnet ”{0}”",
        AlreadyRunning = "Meeting Alarm körs redan (se meddelandefältet).",
        AutostartQuestion = "Starta Meeting Alarm automatiskt med Windows?",
        ConfigCreated = "Inställningsfilen har skapats:\n{0}\n\nFyll i Tenant (t.ex. foretag.se) och LoginHint (din jobbmejl) för varje jobb. " +
                        "Kalendrar från annat håll kan läggas till som ICS-länkar. Spara; ändringar läses in automatiskt.",
        ConfigErrorAtStart = "Fel i {0}:\n\n{1}",
        NoTitle = "(ingen rubrik)",
        UnknownChat = "(chatt)",
        TooManyRequests = "för många förfrågningar, pausar till {0:T}",
    };

    internal static readonly IReadOnlyDictionary<string, Texts> All = new Dictionary<string, Texts>(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = EnUs,
        ["en-GB"] = EnGb,
        ["nl-NL"] = NlNl,
        ["sv-SE"] = SvSe,
    };

    public static Texts T { get; private set; } = EnUs;
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Pick the language: "nl-NL", "sv", "en-GB", … or empty for the Windows display language.
    /// Exact match, otherwise the same language (nl-BE → nl-NL, en-AU → en-GB), otherwise en-US.
    /// </summary>
    public static void UseLanguage(string? language) => (Culture, T) = Resolve(language, CultureInfo.CurrentUICulture);

    internal static (CultureInfo, Texts) Resolve(string? language, CultureInfo system)
    {
        var requested = system;
        if (!string.IsNullOrWhiteSpace(language))
        {
            // predefinedOnly: otherwise any well-formed tag ("not-a-language") yields a made-up culture.
            try { requested = CultureInfo.GetCultureInfo(language.Trim(), predefinedOnly: true); }
            catch (CultureNotFoundException) { }
        }

        var name = All.ContainsKey(requested.Name) ? requested.Name
            : requested.TwoLetterISOLanguageName switch
            {
                "nl" => "nl-NL",
                "sv" => "sv-SE",
                "en" => requested.Name is "en" or "en-US" or "en-CA" or "en-PH" or "en-029" ? "en-US" : "en-GB",
                _ => "en-US",
            };
        return (CultureInfo.GetCultureInfo(name), All[name]);
    }

    /// <summary>Format in the chosen culture (times, numbers).</summary>
    public static string F(string format, params object?[] args) => string.Format(Culture, format, args);
}
