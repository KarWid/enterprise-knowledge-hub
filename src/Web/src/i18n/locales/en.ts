const en = {
  app: {
    title: "Enterprise Knowledge Hub",
    pleaseWait: "Please wait...",
  },
  auth: {
    logIn: "Log in",
    logOut: "Log out",
    signInPrompt: "Sign in to continue.",
    currentUserLoadFailure: "Something went wrong. Please try again or log out.",
  },
  errorModal: {
    retry: "Retry",
    close: "Close",
    genericMessage: "Something went wrong. Please try again.",
  },
  authenticated: {
    welcome: "Welcome!",
  },
  onboarding: {
    createOrganization: "Create your organization",
    createOrganizationSubtitle: "Set up your organization to get started.",
    companyNameLabel: "Company Name",
    companyNamePlaceholder: "e.g. Acme Corp",
    createOrganizationButton: "Create organization",
    createOrganizationError: "Something went wrong. Please try again.",
    acceptInvitationHeading: "You have a pending invitation",
    acceptInvitationSubtitle:
      "Accept an invitation below to join the organization.",
    acceptInvitationButton: "Accept invitation",
    acceptInvitationError: "Something went wrong. Please try again.",
    accessDeniedHeading: "You don't have access to EnterpriseKnowledgeHub yet.",
    accessDeniedMessage:
      "Your account hasn't been invited to an organization. Please contact your organization administrator.",
  },
  nav: {
    main: "Menu",
    chats: "Chats",
    documents: "Documents",
  },
  documents: {
    title: "Documents",
    subtitle: "Upload and manage PDF knowledge sources for your organization.",
    uploadTitle: "Upload a document",
    uploadDescription: "PDF files up to 25 MB are supported.",
    chooseFile: "Choose a PDF file",
    upload: "Upload",
    uploading: "Uploading...",
    listTitle: "Your documents",
    empty: "No documents have been uploaded yet.",
    uploadedAt: "Uploaded {{date}}",
    error: "We couldn't load or upload documents. Please try again.",
    status: {
      PendingForUpload: "Waiting for upload",
      Uploaded: "Uploaded",
      Processing: "Processing",
      Ready: "Ready",
      Failed: "Failed",
      Archived: "Archived",
    },
  },
  organization: {
    selectLabel: "Select organization",
  },
  health: {
    unreachable: "API unreachable",
    checking: "Checking API\u2026",
    status: "API: {{status}} | DB: {{database}}",
  },
  landing: {
    nav: {
      logIn: "Log in",
    },
    hero: {
      headline: "Your company knowledge, instantly accessible.",
      subheadline:
        "Ask questions about your documents, meetings and internal processes — and get answers in seconds.",
      cta: "Get started",
    },
    howItWorks: {
      title: "How it works",
      step1: "Connect",
      step2: "Ask",
      step3: "Get answers",
    },
    useCases: {
      title: "What can you use it for?",
      documents: {
        title: "Documents",
        description: "Find information across your company documentation.",
      },
      meetings: {
        title: "Meetings",
        description: "Turn conversations into searchable knowledge.",
      },
      processes: {
        title: "Processes",
        description:
          "Quickly find out how things are done in your organization.",
      },
    },
    cta: {
      headline: "Stop searching. Start asking.",
      button: "Get started",
    },
    footer: {
      copyright: "© 2026 Enterprise Knowledge Hub. All rights reserved.",
    },
    chatMockup: {
      userMessage: "What is our vacation policy?",
      aiMessage:
        "Based on your HR documentation, employees are entitled to 26 days of paid leave per year. Unused days can be carried over to the next year, up to a maximum of 10 days.",
      aiLabel: "Knowledge Hub",
    },
  },
} as const;

export default en;
