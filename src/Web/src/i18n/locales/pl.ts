const pl = {
  app: {
    title: "Enterprise Knowledge Hub",
    pleaseWait: "Proszę czekać...",
  },
  auth: {
    logIn: "Zaloguj się",
    logOut: "Wyloguj się",
    signInPrompt: "Zaloguj się, aby kontynuować.",
    currentUserLoadFailure: "Coś poszło nie tak. Spróbuj ponownie lub się wyloguj.",
  },
  errorModal: {
    retry: "Spróbuj ponownie",
    close: "Zamknij",
    genericMessage: "Coś poszło nie tak. Spróbuj ponownie.",
  },
  authenticated: {
    welcome: "Witaj!",
  },
  onboarding: {
    createOrganization: "Utwórz organizację",
    createOrganizationSubtitle: "Skonfiguruj swoją organizację, aby rozpocząć.",
    companyNameLabel: "Nazwa firmy",
    companyNamePlaceholder: "np. Acme Corp",
    createOrganizationButton: "Utwórz organizację",
    createOrganizationError: "Coś poszło nie tak. Spróbuj ponownie.",
    acceptInvitationHeading: "Masz oczekujące zaproszenie",
    acceptInvitationSubtitle:
      "Zaakceptuj zaproszenie poniżej, aby dołączyć do organizacji.",
    acceptInvitationButton: "Zaakceptuj zaproszenie",
    acceptInvitationError: "Coś poszło nie tak. Spróbuj ponownie.",
    accessDeniedHeading: "Nie masz jeszcze dostępu do EnterpriseKnowledgeHub.",
    accessDeniedMessage:
      "Twoje konto nie zostało zaproszone do żadnej organizacji. Skontaktuj się z administratorem swojej organizacji.",
  },
  nav: {
    main: "Menu",
    chats: "Czaty",
    documents: "Dokumenty",
  },
  documents: {
    title: "Dokumenty",
    subtitle: "Przeslij i zarzadzaj zrodlami wiedzy PDF dla organizacji.",
    uploadTitle: "Przeslij dokument",
    uploadDescription: "Obslugiwane sa pliki PDF do 25 MB.",
    chooseFile: "Wybierz plik PDF",
    upload: "Przeslij",
    uploading: "Przesylanie...",
    listTitle: "Twoje dokumenty",
    empty: "Nie przeslano jeszcze zadnych dokumentow.",
    uploadedAt: "Przeslano {{date}}",
    error: "Nie mozna wczytac lub przeslac dokumentow. Sprobuj ponownie.",
    status: {
      PendingForUpload: "Oczekuje na przeslanie",
      Uploaded: "Przeslany",
      Processing: "Przetwarzanie",
      Ready: "Gotowy",
      Failed: "Nieudany",
      Archived: "Zarchiwizowany",
    },
  },
  organization: {
    selectLabel: "Wybierz organizację",
  },
  health: {
    unreachable: "API niedostępne",
    checking: "Sprawdzanie API\u2026",
    status: "API: {{status}} | DB: {{database}}",
  },
  landing: {
    nav: {
      logIn: "Zaloguj się",
    },
    hero: {
      headline: "Wiedza Twojej firmy, dostępna natychmiast.",
      subheadline:
        "Zadawaj pytania dotyczące dokumentów, spotkań i wewnętrznych procesów — i otrzymuj odpowiedzi w kilka sekund.",
      cta: "Zacznij",
    },
    howItWorks: {
      title: "Jak to działa",
      step1: "Połącz",
      step2: "Zapytaj",
      step3: "Otrzymaj odpowiedzi",
    },
    useCases: {
      title: "Do czego możesz tego używać?",
      documents: {
        title: "Dokumenty",
        description: "Znajdź informacje w dokumentacji firmowej.",
      },
      meetings: {
        title: "Spotkania",
        description: "Zamień rozmowy w przeszukiwalną wiedzę.",
      },
      processes: {
        title: "Procesy",
        description: "Szybko dowiedz się, jak działa Twoja organizacja.",
      },
    },
    cta: {
      headline: "Przestań szukać. Zacznij pytać.",
      button: "Zacznij",
    },
    footer: {
      copyright: "© 2026 Enterprise Knowledge Hub. Wszelkie prawa zastrzeżone.",
    },
    chatMockup: {
      userMessage: "Jaka jest polityka urlopowa?",
      aiMessage:
        "Na podstawie dokumentacji HR pracownicy mają prawo do 26 dni płatnego urlopu w roku. Niewykorzystane dni można przenosić na kolejny rok, maksymalnie 10 dni.",
      aiLabel: "Knowledge Hub",
    },
  },
} as const;

export default pl;
