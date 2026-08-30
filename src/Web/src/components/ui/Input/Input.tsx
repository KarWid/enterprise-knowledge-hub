import { useField } from 'formik';
import styles from './Input.module.less';

interface InputProps {
  name: string;
  id?: string;
  type?: string;
  placeholder?: string;
  disabled?: boolean;
  maxLength?: number;
  autoFocus?: boolean;
}

export function Input({ name, id, ...rest }: InputProps) {
  const [field, meta] = useField(name);
  const inputId = id ?? name;
  const hasError = meta.touched && !!meta.error;

  return (
    <div className={`Input--container`}>
      <input
        id={inputId}
        className={`${styles.input}${hasError ? ` ${styles.inputError}` : ''}`}
        {...field}
        {...rest}
      />
      {hasError && (
        <p className={styles.errorMessage} role="alert">
          {meta.error}
        </p>
      )}
    </div>
  );
}
