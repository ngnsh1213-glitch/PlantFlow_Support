import * as React from "react"
import { motion } from "framer-motion"

interface SwitchProps {
    checked: boolean;
    onCheckedChange: (checked: boolean) => void;
    className?: string;
    disabled?: boolean;
}

const Switch = React.forwardRef<HTMLButtonElement, SwitchProps>(
    ({ checked, onCheckedChange, disabled, className }, ref) => {
        return (
            <button
                type="button"
                role="switch"
                aria-checked={checked}
                disabled={disabled}
                onClick={() => onCheckedChange(!checked)}
                className={`peer inline-flex h-[24px] w-[44px] shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2 focus-visible:ring-offset-white disabled:cursor-not-allowed disabled:opacity-50 ${checked ? 'bg-blue-600' : 'bg-slate-300'} ${className || ''}`}
                style={{ justifyContent: checked ? "flex-end" : "flex-start" }}
                ref={ref}
            >
                <motion.span
                    layout
                    transition={{ type: "spring", stiffness: 700, damping: 30 }}
                    className="pointer-events-none block h-5 w-5 rounded-full bg-white shadow-md ring-0"
                />
            </button>
        )
    }
)
Switch.displayName = "Switch"

export { Switch }
