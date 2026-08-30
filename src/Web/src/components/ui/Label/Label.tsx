import type { ReactNode } from 'react';
import styles from './Label.module.less';

interface LabelProps {
  htmlFor: string;
  children: ReactNode;
  required?: boolean;
}

export function Label({ htmlFor, children, required }: LabelProps) {
  return (
    <label htmlFor={htmlFor} className={styles.label}>
      {children}
      {required && <span className={styles.required} aria-hidden="true"> *</span>}
    </label>
  );
}
