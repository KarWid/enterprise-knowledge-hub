import { useState } from "react";
import { useTranslation } from "react-i18next";
import { TextPreviewModal } from "../../../../components/ui/TextPreviewModal/TextPreviewModal";
import { useGetDocumentChunksQuery } from "../../../../services/api/generated/api";
import { useAppSelector } from "../../../../services/hooks";
import { selectCurrentOrganizationId } from "../../../../services/organization/organizationSlice";

interface DocumentChunkPreviewProps {
  documentId: string;
  onClose: () => void;
}

export function DocumentChunkPreview({
  documentId,
  onClose,
}: DocumentChunkPreviewProps) {
  const { t } = useTranslation();
  const [offset, setOffset] = useState(0);
  const organizationId = useAppSelector(selectCurrentOrganizationId);

  const preview = useGetDocumentChunksQuery(
    { documentId, offset, limit: 1, "X-Organization-Id": organizationId ?? "" },
    { skip: !organizationId, refetchOnMountOrArgChange: true },
  );

  const data = preview.currentData;
  const chunk = data?.items[0];

  const caption =
    chunk && data
      ? t("documents.chunking.position", {
          index: chunk.ordinal + 1,
          count: data.chunkCount,
        }) + (chunk.isTruncated ? ` ${t("documents.chunking.truncated")}` : "")
      : undefined;

  return (
    <TextPreviewModal
      open
      title={t("documents.chunking.previewTitle")}
      onClose={onClose}
      text={data ? (chunk?.content ?? "") : undefined}
      caption={caption}
      isLoading={preview.isFetching}
      errorMessage={
        preview.error !== undefined
          ? t("documents.chunking.previewError")
          : undefined
      }
      retryText={t("errorModal.retry")}
      onRetry={() => {
        void preview.refetch();
      }}
      previousButton={{
        text: t("documents.chunking.previous"),
        disabled: offset === 0,
        onClick: () => setOffset((current) => current - 1),
      }}
      nextButton={{
        text: t("documents.chunking.next"),
        disabled: !data?.hasMore,
        onClick: () => setOffset((current) => current + 1),
      }}
    />
  );
}
