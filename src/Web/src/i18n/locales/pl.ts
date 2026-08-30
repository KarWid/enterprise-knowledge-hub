const pl = {
  app: {
    title: 'Enterprise Knowledge Hub',
    pleaseWait: 'Proszę czekać...',
  },
  auth: {
    logIn: 'Zaloguj się',
    logOut: 'Wyloguj się',
    signInPrompt: 'Zaloguj się, aby kontynuować.',
  },
  authenticated: {
    welcome: 'Witaj!',
  },
  onboarding: {
    createOrganization: 'Utwórz organizację',
    createOrganizationSubtitle: 'Skonfiguruj swoją organizację, aby rozpocząć.',
    companyNameLabel: 'Nazwa firmy',
    companyNamePlaceholder: 'np. Acme Corp',
    createOrganizationButton: 'Utwórz organizację',
    createOrganizationError: 'Coś poszło nie tak. Spróbuj ponownie.',
    acceptInvitationErrorHeading: 'Zaproszenia nieobsługiwane',
    acceptInvitationError: 'Dołączanie przez zaproszenie nie jest jeszcze dostępne. Skontaktuj się z administratorem.',
  },
  nav: {
    main: 'Menu',
    chats: 'Czaty',
    documents: 'Dokumenty',
  },
  health: {
    unreachable: 'API niedostępne',
    checking: 'Sprawdzanie API\u2026',
    status: 'API: {{status}} | DB: {{database}}',
  },  
  landing: {
    nav: {
      logIn: 'Zaloguj się',
    },
    hero: {
      headline: 'Wiedza Twojej firmy, dostępna natychmiast.',
      subheadline:
        'Zadawaj pytania dotyczące dokumentów, spotkań i wewnętrznych procesów — i otrzymuj odpowiedzi w kilka sekund.',
      cta: 'Zacznij',
    },
    howItWorks: {
      title: 'Jak to działa',
      step1: 'Połącz',
      step2: 'Zapytaj',
      step3: 'Otrzymaj odpowiedzi',
    },
    useCases: {
      title: 'Do czego możesz tego używać?',
      documents: {
        title: 'Dokumenty',
        description: 'Znajdź informacje w dokumentacji firmowej.',
      },
      meetings: {
        title: 'Spotkania',
        description: 'Zamień rozmowy w przeszukiwalną wiedzę.',
      },
      processes: {
        title: 'Procesy',
        description: 'Szybko dowiedz się, jak działa Twoja organizacja.',
      },
    },
    cta: {
      headline: 'Przestań szukać. Zacznij pytać.',
      button: 'Zacznij',
    },
    footer: {
      copyright: '© 2026 Enterprise Knowledge Hub. Wszelkie prawa zastrzeżone.',
    },
    chatMockup: {
      userMessage: 'Jaka jest polityka urlopowa?',
      aiMessage:
        'Na podstawie dokumentacji HR pracownicy mają prawo do 26 dni płatnego urlopu w roku. Niewykorzystane dni można przenosić na kolejny rok, maksymalnie 10 dni.',
      aiLabel: 'Knowledge Hub',
    },
  },} as const;

export default pl;
