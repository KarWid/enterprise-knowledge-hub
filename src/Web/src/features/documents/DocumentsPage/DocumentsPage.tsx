import { useTranslation } from "react-i18next";
import { ErrorModal } from "../../../components/ErrorModal/ErrorModal";
import { Button } from "../../../components/ui/Button/Button";
import { Card } from "../../../components/ui/Card/Card";
import { FilePicker } from "../../../components/ui/FilePicker/FilePicker";
import { Heading } from "../../../components/ui/Heading/Heading";
import { Section } from "../../../components/ui/Section/Section";
import { useGetDocumentsQuery } from "../../../services/api/generated/api";
import { DocumentListItem } from "../components/DocumentListItem/DocumentListItem";
import { useDocumentUpload } from "../services/useDocumentUpload";
import styles from "./DocumentsPage.module.less";

export function DocumentsPage() {
  const { t } = useTranslation();
  const { data: documents = [], error: documentsError, isLoading, refetch } =
    useGetDocumentsQuery();
  const upload = useDocumentUpload({ onCompleted: () => void refetch() });

  function retry() {
    if (upload.error !== undefined && upload.file !== null) {
      upload.retryUpload();
      return;
    }

    void refetch();
  }

  return (
    <Section>
      <Heading
        level="h1"
        title={t("documents.title")}
        subtitle={t("documents.subtitle")}
        className={styles.header}
      />

      <Card className={styles.uploadCard}>
        <Heading
          level="h2"
          title={t("documents.uploadTitle")}
          subtitle={t("documents.uploadDescription")}
        />
        <div className={styles.uploadControls}>
          <FilePicker
            id="document-upload-file"
            selectedFileName={upload.file?.name}
            placeholder={t("documents.chooseFile")}
            accept="application/pdf,.pdf"
            onFileChange={upload.setFile}
            disabled={upload.isUploading}
          />
          <Button
            type="button"
            className={styles.uploadButton}
            onClick={() => void upload.uploadSelectedFile()}
            disabled={upload.file === null || upload.isUploading}
          >
            {upload.isUploading ? t("documents.uploading") : t("documents.upload")}
          </Button>
        </div>
      </Card>

      <Card>
        <Heading level="h2" title={t("documents.listTitle")} />
        {isLoading ? (
          <p className={styles.muted}>{t("app.pleaseWait")}</p>
        ) : documents.length === 0 ? (
          <p className={styles.muted}>{t("documents.empty")}</p>
        ) : (
          <ul className={styles.documentList}>
            {documents.map((document) => (
              <DocumentListItem key={document.id} document={document} />
            ))}
          </ul>
        )}
      </Card>

      <ErrorModal
        error={upload.error ?? documentsError}
        fallbackMessage={t("documents.error")}
        onContinue={retry}
      />
    </Section>
  );
}
