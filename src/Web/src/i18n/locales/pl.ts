const pl = {
  app: {
    title: 'Enterprise Knowledge Hub',
  },
  auth: {
    logIn: 'Zaloguj się',
    logOut: 'Wyloguj się',
    signInPrompt: 'Zaloguj się, aby kontynuować.',
  },
  authenticated: {
    welcome: 'Witaj!',
  },
  health: {
    unreachable: 'API niedostępne',
    checking: 'Sprawdzanie API\u2026',
    status: 'API: {{status}} | DB: {{database}}',
  },
} as const;

export default pl;
