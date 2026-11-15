import { Dropdown as AntDropdown, type MenuProps } from "antd";
import { useState, type ReactNode } from "react";
import { ChevronDownIcon } from "../../icons";
import styles from "./Dropdown.module.less";

export interface DropdownOption {
  label: string;
  value: string;
}

interface DropdownProps {
  options: DropdownOption[];
  value?: string;
  onChange: (value: string) => void;
  ariaLabel?: string;
  title?: string;
  className?: string;
  triggerClassName?: string;
  triggerContent?: ReactNode;
  disabled?: boolean;
}

export function Dropdown({
  options,
  value,
  onChange,
  ariaLabel,
  title,
  className,
  triggerClassName,
  triggerContent,
  disabled = false,
}: DropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = options.find((option) => option.value === value);
  const items: MenuProps["items"] = options.map((option) => ({
    key: option.value,
    label: option.label,
  }));

  function handleSelect({ key }: { key: string }) {
    if (disabled) {
      return;
    }
    onChange(key);
    setIsOpen(false);
  }

  return (
    <div className={`${styles.dropdown}${className ? ` ${className}` : ""}`}>
      <AntDropdown
        disabled={disabled}
        open={!disabled && isOpen}
        onOpenChange={setIsOpen}
        trigger={["click"]}
        placement="bottomRight"
        autoFocus
        destroyOnHidden
        classNames={{ root: styles.dropdownContainer }}
        menu={{
          items,
          selectable: value !== undefined,
          selectedKeys: value === undefined ? [] : [value],
          onClick: handleSelect,
        }}
      >
        <button
          type="button"
          disabled={disabled}
          className={`${styles.trigger}${triggerClassName ? ` ${triggerClassName}` : ""}`}
          aria-haspopup="menu"
          aria-expanded={!disabled && isOpen}
          aria-label={ariaLabel}
          title={title}
        >
          {triggerContent ?? (
            <>
              <span className={styles.triggerLabel}>{selectedOption?.label}</span>
              <ChevronDownIcon
                className={`${styles.chevron}${isOpen ? ` ${styles.chevronOpen}` : ""}`}
              />
            </>
          )}
        </button>
      </AntDropdown>
    </div>
  );
}
