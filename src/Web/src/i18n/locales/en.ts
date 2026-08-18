const en = {
  app: {
    title: 'Enterprise Knowledge Hub',
    pleaseWait: 'Please wait...',
  },
  auth: {
    logIn: 'Log in',
    logOut: 'Log out',
    signInPrompt: 'Sign in to continue.',
  },
  authenticated: {
    welcome: 'Welcome!',
  },
  health: {
    unreachable: 'API unreachable',
    checking: 'Checking API\u2026',
    status: 'API: {{status}} | DB: {{database}}',
  },  
  landing: {
    nav: {
      logIn: 'Log in',
    },
    hero: {
      headline: 'Your company knowledge, instantly accessible.',
      subheadline:
        'Ask questions about your documents, meetings and internal processes — and get answers in seconds.',
      cta: 'Get started',
    },
    howItWorks: {
      title: 'How it works',
      step1: 'Connect',
      step2: 'Ask',
      step3: 'Get answers',
    },
    useCases: {
      title: 'What can you use it for?',
      documents: {
        title: 'Documents',
        description: 'Find information across your company documentation.',
      },
      meetings: {
        title: 'Meetings',
        description: 'Turn conversations into searchable knowledge.',
      },
      processes: {
        title: 'Processes',
        description: 'Quickly find out how things are done in your organization.',
      },
    },
    cta: {
      headline: 'Stop searching. Start asking.',
      button: 'Get started',
    },
    footer: {
      copyright: '© 2026 Enterprise Knowledge Hub. All rights reserved.',
    },
    chatMockup: {
      userMessage: 'What is our vacation policy?',
      aiMessage:
        'Based on your HR documentation, employees are entitled to 26 days of paid leave per year. Unused days can be carried over to the next year, up to a maximum of 10 days.',
      aiLabel: 'Knowledge Hub',
    },
  },} as const;

export default en;
