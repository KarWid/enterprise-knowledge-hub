import { useTranslation } from "react-i18next";
import { useState } from "react";
import { MoreVerticalIcon } from "../../../../components/icons";
import { Dropdown, type DropdownOption } from "../../../../components/ui/Dropdown/Dropdown";
import type { DocumentResponse } from "../../../../services/api/generated/api";
import { DocumentTextPreview } from "../DocumentTextPreview/DocumentTextPreview";
import { DocumentChunkPreview } from "../DocumentChunkPreview/DocumentChunkPreview";
import { DocumentStatusIndicator } from "../DocumentStatusIndicator/DocumentStatusIndicator";
import styles from "./DocumentListItem.module.less";

interface DocumentListItemProps {
  document: DocumentResponse;
}

export function DocumentListItem({ document }: DocumentListItemProps) {
  const { t } = useTranslation();
  const [previewOpen, setPreviewOpen] = useState(false);
  const [chunksOpen, setChunksOpen] = useState(false);
  const createdAt = new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(document.createdAt));
  const actions: DropdownOption[] = [];

  // Chunking metadata is included only in the authorized diagnostics view.
  if (document.chunking && document.textExtraction?.status === "Completed") {
    actions.push({ value: "content", label: t("documents.extraction.viewText") });
  }

  if (document.chunking?.status === "Completed") {
    actions.push({ value: "chunks", label: t("documents.chunking.viewChunks") });
  }

  function handleAction(action: string) {
    if (action === "content") {
      setPreviewOpen(true);
    } else if (action === "chunks") {
      setChunksOpen(true);
    }
  }

  return (
    <li className={styles.document}>
      <span className={styles.documentIcon} aria-hidden="true">
        {document.name.split(".").pop()?.toUpperCase()}
      </span>
      <div className={styles.details}>
        <strong>{document.name}</strong>
        <span className={styles.documentMeta}>
          {t("documents.uploadedAt", { date: createdAt })}
        </span>
      </div>
      <div className={styles.controls}>
        <DocumentStatusIndicator document={document} />
        {actions.length > 0 && (
          <Dropdown
            options={actions}
            onChange={handleAction}
            ariaLabel={t("documents.actions", { name: document.name })}
            triggerClassName={styles.actionsTrigger}
            triggerContent={<MoreVerticalIcon />}
          />
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
