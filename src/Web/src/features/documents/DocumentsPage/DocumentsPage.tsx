import { useTranslation } from "react-i18next";
import { ErrorModal } from "../../../components/ErrorModal/ErrorModal";
import { Button } from "../../../components/ui/Button/Button";
import { Card } from "../../../components/ui/Card/Card";
import { FilePicker } from "../../../components/ui/FilePicker/FilePicker";
import { Heading } from "../../../components/ui/Heading/Heading";
import { InlineMessage } from "../../../components/ui/InlineMessage/InlineMessage";
import { Modal } from "../../../components/ui/Modal/Modal";
import { Section } from "../../../components/ui/Section/Section";
import { DocumentList } from "../components/DocumentList/DocumentList";
import { DocumentUploadNotice } from "../components/DocumentUploadNotice/DocumentUploadNotice";
import { useDocumentsPage } from "../services/useDocumentsPage";
import styles from "./DocumentsPage.module.less";

export function DocumentsPage() {
  const { t } = useTranslation();
  const { documents, isLoading, upload, error, retry } = useDocumentsPage();

  return (
    <Section>
      <Heading
        level="h1"
        title={t("documents.title")}
        subtitle={t("documents.subtitle")}
        className={styles.header}
      />

      <Card className={styles.uploadCard}>
        <div className={styles.uploadRow}>
          <Heading
            level="h2"
            title={t("documents.uploadTitle")}
            subtitle={upload.description}
          />
          <div className={styles.uploadControls}>
            <FilePicker
              id="document-upload-file"
              selectedFileName={upload.file?.name}
              placeholder={t("documents.chooseFile")}
              accept={upload.accept}
              onFileChange={upload.selectFile}
              disabled={!upload.canSelectFile}
            />
            <Button
              type="button"
              className={styles.uploadButton}
              onClick={upload.submit}
              disabled={!upload.canSubmit}
            >
              {upload.isUploading ? t("documents.uploading") : t("documents.upload")}
            </Button>
          </div>
        </div>
        {upload.showValidationError && (
          <InlineMessage variant="error">{t("documents.invalidFile")}</InlineMessage>
        )}
      </Card>

      <DocumentUploadNotice notice={upload.notice} className={styles.uploadNotice} />

      <Card>
        <Heading level="h2" title={t("documents.listTitle")} />
        <DocumentList documents={documents} isLoading={isLoading} />
      </Card>

      <ErrorModal
        error={error}
        fallbackMessage={t("documents.error")}
        onContinue={retry}
      />

      <Modal
        open={upload.isUploading}
        title={t("documents.uploadProgressTitle")}
        isProgress
        text={t("documents.uploadProgressMessage")}
      />
    </Section>
  );
}
