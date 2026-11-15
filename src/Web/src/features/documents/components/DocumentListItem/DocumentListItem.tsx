import { useTranslation } from "react-i18next";
import { useState } from "react";
import { Button } from "../../../../components/ui/Button/Button";
import type { DocumentResponse } from "../../../../services/api/generated/api";
import { DocumentTextPreview } from "../DocumentTextPreview/DocumentTextPreview";
import { DocumentChunkPreview } from "../DocumentChunkPreview/DocumentChunkPreview";
import styles from "./DocumentListItem.module.less";

interface DocumentListItemProps {
  document: DocumentResponse;
}

// TODO @KWidla: statuses should be represented by some smaller components or icons
export function DocumentListItem({ document }: DocumentListItemProps) {
  const { t } = useTranslation();
  const [previewOpen, setPreviewOpen] = useState(false);
  const [chunksOpen, setChunksOpen] = useState(false);
  const createdAt = new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(document.createdAt));

  return (
    <li className={styles.document}>
      <span className={styles.documentIcon} aria-hidden="true">
        {document.name.split(".").pop()?.toUpperCase()}
      </span>
      <div>
        <strong>{document.name}</strong>
        <span className={styles.documentMeta}>
          {t("documents.uploadedAt", { date: createdAt })}
        </span>
      </div>
      <div className={styles.status}>
        <span>{t(`documents.status.${document.status}`)}</span>
        {document.textExtraction && (
          <span className={styles.documentMeta}>
            {t(`documents.extraction.status.${document.textExtraction.status}`)}
          </span>
        )}
        {document.textExtraction?.status === "Completed" && (
          <Button variant="secondary" onClick={() => setPreviewOpen(true)}>
            {t("documents.extraction.viewText")}
          </Button>
        )}
        {document.chunking && (
          <span className={styles.documentMeta}>
            {t(`documents.chunking.status.${document.chunking.status}`)}
          </span>
        )}
        {document.chunking?.status === "Completed" && (
          <Button variant="secondary" onClick={() => setChunksOpen(true)}>
            {t("documents.chunking.viewChunks")}
          </Button>
        )}
      </div>
      <DocumentTextPreview
        documentId={document.id}
        open={previewOpen}
        onClose={() => setPreviewOpen(false)}
      />
      {chunksOpen && (
        <DocumentChunkPreview
          documentId={document.id}
          onClose={() => setChunksOpen(false)}
        />
      )}
    </li>
  );
}
