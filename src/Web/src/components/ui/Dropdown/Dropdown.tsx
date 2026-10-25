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
  value: string;
  onChange: (value: string) => void;
  ariaLabel?: string;
  title?: string;
  className?: string;
  triggerClassName?: string;
  triggerContent?: ReactNode;
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
}: DropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = options.find((option) => option.value === value);
  const items: MenuProps["items"] = options.map((option) => ({
    key: option.value,
    label: option.label,
  }));

  function handleSelect({ key }: { key: string }) {
    onChange(key);
    setIsOpen(false);
  }

  return (
    <div className={`${styles.dropdown}${className ? ` ${className}` : ""}`}>
      <AntDropdown
        open={isOpen}
        onOpenChange={setIsOpen}
        trigger={["click"]}
        placement="bottomRight"
        autoFocus
        destroyOnHidden
        classNames={{ root: styles.dropdownContainer }}
        menu={{
          items,
          selectable: true,
          selectedKeys: [value],
          onClick: handleSelect,
        }}
      >
        <button
          type="button"
          className={`${styles.trigger}${triggerClassName ? ` ${triggerClassName}` : ""}`}
          aria-haspopup="menu"
          aria-expanded={isOpen}
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
