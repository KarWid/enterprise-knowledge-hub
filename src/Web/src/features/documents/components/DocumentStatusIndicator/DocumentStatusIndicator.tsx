import { useTranslation } from "react-i18next";
import { InfoIcon } from "../../../../components/icons";
import { Tooltip } from "../../../../components/ui/Tooltip/Tooltip";
import type { DocumentResponse } from "../../../../services/api/generated/api";
import styles from "./DocumentStatusIndicator.module.less";

interface DocumentStatusIndicatorProps {
  document: DocumentResponse;
}

type StatusTone = "success" | "processing" | "warning" | "error" | "neutral";

export function DocumentStatusIndicator({
  document,
}: DocumentStatusIndicatorProps) {
  const { t } = useTranslation();
  const stages = [
    {
      status: document.status,
      translation: `documents.status.${document.status}`,
    },
    {
      status: document.textExtraction?.status,
      translation: `documents.extraction.status.${document.textExtraction?.status}`,
    },
    {
      status: document.chunking?.status,
      translation: `documents.chunking.status.${document.chunking?.status}`,
    },
  ].filter((stage) => stage.status && stage.status !== "Disabled");

  if (stages.length === 0) {
    return null;
  }

  const description = stages.map((stage) => t(stage.translation)).join("\n");
  const tone = getStatusTone(stages.map((stage) => stage.status!));

  return (
    <Tooltip content={description}>
      <span
        className={`${styles.indicator} ${styles[tone]}`}
        role="img"
        tabIndex={0}
        aria-label={description}
      >
        <InfoIcon />
      </span>
    </Tooltip>
  );
}

function getStatusTone(statuses: string[]): StatusTone {
  if (
    statuses.some((status) =>
      ["Rejected", "Failed", "DeadLetter"].includes(status),
    )
  ) {
    return "error";
  }

  if (statuses.includes("Paused")) {
    return "warning";
  }

  if (
    statuses.some((status) =>
      ["PendingForUpload", "Pending", "Processing"].includes(status),
    )
  ) {
    return "processing";
  }

  if (
    statuses.every((status) =>
      ["Uploaded", "Ready", "Completed"].includes(status),
    )
  ) {
    return "success";
  }

  return "neutral";
}
