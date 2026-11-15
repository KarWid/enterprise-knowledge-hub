import { useTranslation } from "react-i18next";
import { TextPreviewModal } from "../../../../components/ui/TextPreviewModal/TextPreviewModal";
import { useGetDocumentTextPreviewQuery } from "../../../../services/api/generated/api";
import { useAppSelector } from "../../../../services/hooks";
import { selectCurrentOrganizationId } from "../../../../services/organization/organizationSlice";

interface DocumentTextPreviewProps {
  documentId: string;
  open: boolean;
  onClose: () => void;
}

export function DocumentTextPreview({
  documentId,
  open,
  onClose,
}: DocumentTextPreviewProps) {
  const { t } = useTranslation();
  const organizationId = useAppSelector(selectCurrentOrganizationId);
  const preview = useGetDocumentTextPreviewQuery(
    { documentId, "X-Organization-Id": organizationId ?? "" },
    { skip: !open || !organizationId, refetchOnMountOrArgChange: true },
  );
  const previewData = preview.currentData;
  const errorMessage =
    preview.error !== undefined
      ? t("documents.extraction.previewError")
      : undefined;
  const caption = previewData
    ? t("documents.extraction.characterCount", {
        count: previewData.characterCount,
      }) +
      (previewData.isTruncated ? ` ${t("documents.extraction.truncated")}` : "")
    : undefined;

  function retry() {
    void preview.refetch();
  }

  return (
    <TextPreviewModal
      open={open}
      title={t("documents.extraction.previewTitle")}
      onClose={onClose}
      text={previewData?.text}
      caption={caption}
      isLoading={preview.isFetching}
      errorMessage={errorMessage}
      retryText={t("errorModal.retry")}
      onRetry={retry}
    />
  );
}
