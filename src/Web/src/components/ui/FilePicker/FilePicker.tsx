import styles from "./FilePicker.module.less";

interface FilePickerProps {
  id: string;
  selectedFileName?: string;
  placeholder: string;
  accept?: string;
  disabled?: boolean;
  onFileChange: (file: File | null) => void;
}

export function FilePicker({
  id,
  selectedFileName,
  placeholder,
  accept,
  disabled = false,
  onFileChange,
}: FilePickerProps) {
  return (
    <label
      htmlFor={id}
      className={`${styles.filePicker}${disabled ? ` ${styles.disabled}` : ""}`}
    >
      <span className={styles.fileName}>{selectedFileName || placeholder}</span>
      <input
        id={id}
        className={styles.input}
        type="file"
        accept={accept}
        disabled={disabled}
        onChange={(event) => {
          onFileChange(event.target.files?.[0] ?? null);
          event.target.value = "";
        }}
      />
    </label>
  );
}
