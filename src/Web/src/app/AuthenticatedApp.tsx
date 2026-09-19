import { useTranslation } from "react-i18next";
import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { UserInfo } from "../components/UserInfo/UserInfo";
import styles from "./AuthenticatedApp.module.less";
import { useGetMeQuery } from "../services/api/generated/api";
import { UserOnboardingStatusType } from "../services/api/enums";
import { CreateOrganizationPage } from "../features/onboarding/CreateOrganizationPage";
import { AcceptInvitationPage } from "../features/onboarding/AcceptInvitationPage";
import { AccessDeniedPage } from "../features/onboarding/AccessDeniedPage";
import { ChatsPage } from "../features/chats/ChatsPage";
import { DocumentsPage } from "../features/documents/DocumentsPage";
import { AuthLoadingPage } from "./AuthLoadingPage";
import { ErrorModal } from "../components/ErrorModal/ErrorModal";

export function AuthenticatedApp() {
  const { t } = useTranslation();
  const { data, error, refetch } = useGetMeQuery();

  function retryGetMe() {
    void refetch();
  }

  if (data === undefined) {
    return (
      <>
        <AuthLoadingPage message={t("app.pleaseWait")} />
        <ErrorModal
          error={error}
          fallbackMessage={t("errorModal.genericMessage")}
          onContinue={retryGetMe}
        />
      </>
    );
  }

  let content: React.ReactNode;
  switch (data.onboardingStatus) {
    case UserOnboardingStatusType.CreateOrganization:
      content = <CreateOrganizationPage />;
      break;
    case UserOnboardingStatusType.AcceptInvitation:
      content = <AcceptInvitationPage />;
      break;
    case UserOnboardingStatusType.AccessDenied:
      content = <AccessDeniedPage />;
      break;
    default:
      content = <AppShell />;
      break;
  }

  return (
    <>
      {content}
      <ErrorModal
        error={error}
        fallbackMessage={t("errorModal.genericMessage")}
        onContinue={retryGetMe}
      />
    </>
  );
}

function AppShell() {
  const { t } = useTranslation();

  function navClass({ isActive }: { isActive: boolean }) {
    return `${styles.navItem}${isActive ? ` ${styles.navItemActive}` : ""}`;
  }

  return (
    <div className={styles.shell}>
      <header className={styles.topbar}>
        <span className={styles.topbarLogo}>{t("app.title")}</span>
        <UserInfo />
      </header>

      <nav className={styles.sidebar}>
        <div className={styles.navSection}>
          <span className={styles.navLabel}>{t("nav.main")}</span>
          <NavLink to="/chats" className={navClass}>
            <span className={styles.navIcon}>💬</span>
            {t("nav.chats")}
          </NavLink>
          <NavLink to="/documents" className={navClass}>
            <span className={styles.navIcon}>📄</span>
            {t("nav.documents")}
          </NavLink>
        </div>
      </nav>

      <main className={styles.main}>
        <Routes>
          <Route path="/chats" element={<ChatsPage />} />
          <Route path="/documents" element={<DocumentsPage />} />
          <Route path="*" element={<Navigate to="/chats" replace />} />
        </Routes>
      </main>
    </div>
  );
}
