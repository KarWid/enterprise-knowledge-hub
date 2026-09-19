import { useState } from "react";
import { useTranslation } from "react-i18next";
import { LogoutButton } from "../../components/LogoutButton";
import {
  useAcceptOrganizationInvitationMutation,
  useGetMeQuery,
} from "../../services/api/generated/api";
import styles from "./AcceptInvitationPage.module.less";

export function AcceptInvitationPage() {
  const { t } = useTranslation();
  const { data, refetch: refetchMe } = useGetMeQuery();
  const [acceptInvitation, { isLoading }] =
    useAcceptOrganizationInvitationMutation();
  const [acceptingId, setAcceptingId] = useState<string | null>(null);

  const invitations = data?.pendingInvitations ?? [];

  async function handleAccept(invitationId: string) {
    setAcceptingId(invitationId);

    const result = await acceptInvitation({ invitationId });

    setAcceptingId(null);

    if ("error" in result) {
      return;
    }

    refetchMe();
  }

  return (
    <div className={styles.page}>
      <LogoutButton className={styles.logout} />
      <div className={styles.card}>
        <span className={styles.icon} aria-hidden="true">
          ✉️
        </span>
        <h1 className={styles.heading}>
          {t("onboarding.acceptInvitationHeading")}
        </h1>
        <p className={styles.message}>
          {t("onboarding.acceptInvitationSubtitle")}
        </p>

        <ul className={styles.list}>
          {invitations.map((invitation) => (
            <li key={invitation.id} className={styles.listItem}>
              <span className={styles.organizationName}>
                {invitation.organizationName}
              </span>
              <button
                type="button"
                className={styles.button}
                disabled={isLoading}
                onClick={() => invitation.id && handleAccept(invitation.id)}
              >
                {acceptingId === invitation.id
                  ? t("app.pleaseWait")
                  : t("onboarding.acceptInvitationButton")}
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
