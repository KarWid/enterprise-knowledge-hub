const pl = {
  app: {
    title: "Enterprise Knowledge Hub",
    pleaseWait: "Proszę czekać...",
  },
  auth: {
    logIn: "Zaloguj się",
    logOut: "Wyloguj się",
    signInPrompt: "Zaloguj się, aby kontynuować.",
    currentUserLoadFailure:
      "Coś poszło nie tak. Spróbuj ponownie lub się wyloguj.",
  },
  errorModal: {
    retry: "Spróbuj ponownie",
    close: "Zamknij",
    genericMessage: "Coś poszło nie tak. Spróbuj ponownie.",
  },
  authenticated: {
    welcome: "Witaj!",
  },
  language: {
    selectorLabel: "Język",
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
    subtitle: "Przeslij i zarzadzaj zrodlami wiedzy dla organizacji.",
    uploadTitle: "Przeslij dokument",
    uploadDescription: "Obslugiwane pliki: {{formats}}. Maksymalny rozmiar: {{maxSize}} MiB.",
    loadingFormats: "Wczytywanie obslugiwanych formatow...",
    uploadsDisabled: "Przesylanie dokumentow jest obecnie wylaczone.",
    invalidFile: "Wybierz niepusty plik z obslugiwanym rozszerzeniem w podanym limicie rozmiaru.",
    chooseFile: "Wybierz dokument",
    upload: "Przeslij",
    uploading: "Przesylanie...",
    uploadProgressTitle: "Przesyłanie dokumentu",
    uploadProgressMessage:
      "Nie zamykaj tej karty, dopóki przesyłanie nie zostanie zakończone.",
    listTitle: "Twoje dokumenty",
    empty: "Nie przeslano jeszcze zadnych dokumentow.",
    uploadedAt: "Przeslano {{date}}",
    refresh: "Odśwież",
    actions: "Akcje dla {{name}}",
    chunking: {
      viewChunks: "Pokaż fragmenty",
      previewTitle: "Fragmenty dokumentu",
      previewError: "Nie udało się wczytać fragmentów dokumentu.",
      position: "Fragment {{index}} z {{count}}.",
      truncated: "Wyświetlany jest początek tego fragmentu.",
      previous: "Poprzedni",
      next: "Następny",
      status: {
        NotAvailable: "Fragmenty niedostępne",
        Disabled: "Przygotowanie fragmentów wyłączone dla tego formatu",
        Paused: "Przygotowywanie fragmentów wstrzymane",
        Pending: "Oczekuje na przygotowanie fragmentów",
        Processing: "Przygotowywanie fragmentów…",
        Completed: "Fragmenty przygotowane",
        Failed: "Nie udało się przygotować fragmentów",
        DeadLetter: "Przygotowanie fragmentów wymaga interwencji",
      },
    },
    extraction: {
      viewText: "Pokaż podgląd treści",
      previewTitle: "Podgląd treści dokumentu",
      previewError: "Nie udało się wczytać podglądu treści dokumentu.",
      characterCount: "Liczba znaków: {{count}}.",
      truncated: "Wyświetlany jest początek dokumentu.",
      pollingExpired: "Automatyczne odświeżanie wstrzymano. Przygotowywanie treści trwa; odśwież, aby sprawdzić postęp.",
      status: {
        NotAvailable: "Przygotowanie treści niedostępne",
        Disabled: "Przygotowanie treści wyłączone dla tego formatu",
        Paused: "Przygotowywanie treści wstrzymane",
        Pending: "Oczekuje na przygotowanie treści",
        Processing: "Przygotowywanie treści…",
        Completed: "Treść przygotowana",
        Failed: "Nie udało się przygotować treści",
        DeadLetter: "Przygotowanie treści wymaga interwencji",
      },
    },
    error: "Nie mozna wczytac lub przeslac dokumentow. Sprobuj ponownie.",
    status: {
      PendingForUpload: "Oczekuje na weryfikację przesłania",
      Uploaded: "Przesłanie zweryfikowane",
      Rejected: "Odrzucony",
      Processing: "Przetwarzanie",
      Ready: "Gotowy",
      Failed: "Nieudany",
      Archived: "Zarchiwizowany",
    },
    verification: {
      verifying: "Przesłano — trwa weryfikacja… Możesz opuścić tę stronę.",
      pending:
        "Weryfikacja nadal trwa. Sprawdź status później na liście dokumentów.",
      verified:
        "Przesłanie zweryfikowane. Postęp przygotowywania treści znajdziesz na liście dokumentów.",
      rejected:
        "Dokument odrzucony: zawartosc lub rozmiar pliku nie spelnia wymagan przesylania.",
      failed:
        "Nie udało się zweryfikować przesłania. Sprawdź status przed ponowną próbą.",
    },
  },
  organization: {
    switchingLocked: "Poczekaj na zakończenie przesyłania dokumentu przed zmianą organizacji.",
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
