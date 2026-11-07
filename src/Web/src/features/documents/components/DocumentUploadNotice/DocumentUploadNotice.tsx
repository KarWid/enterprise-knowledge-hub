import { useTranslation } from "react-i18next";
import {
  InlineMessage,
  type InlineMessageVariant,
} from "../../../../components/ui/InlineMessage/InlineMessage";
import type { DocumentUploadNotice as UploadNotice } from "../../services/useDocumentUpload";

interface DocumentUploadNoticeProps {
  notice: UploadNotice | undefined;
  className?: string;
}

const variants: Record<UploadNotice, InlineMessageVariant> = {
  verifying: "info",
  verified: "info",
  pending: "warning",
  rejected: "error",
  failed: "error",
};

export function DocumentUploadNotice({ notice, className }: DocumentUploadNoticeProps) {
  const { t } = useTranslation();
  if (notice === undefined) return null;

  return (
    <InlineMessage variant={variants[notice]} className={className}>
      {t(`documents.verification.${notice}`)}
    </InlineMessage>
  );
}
