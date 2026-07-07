import * as React from "react"
import { HTMLMotionProps, motion } from "framer-motion"

export interface ButtonProps extends HTMLMotionProps<"button"> {
    variant?: 'default' | 'outline' | 'ghost' | 'destructive'
    size?: 'default' | 'sm' | 'lg' | 'icon'
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
    ({ className, variant = 'default', size = 'default', disabled, ...props }, ref) => {
        const baseStyles = "inline-flex items-center justify-center rounded-xl text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/30 disabled:opacity-50 disabled:pointer-events-none"

        let variantStyles = "bg-blue-600 text-white hover:bg-blue-700 shadow-sm shadow-blue-200"
        if (variant === 'outline') variantStyles = "border border-slate-200/80 bg-white/60 backdrop-blur-sm hover:bg-slate-50 hover:text-slate-900 shadow-[0_2px_10px_-3px_rgba(0,0,0,0.03)]"
        if (variant === 'ghost') variantStyles = "hover:bg-slate-100 hover:text-slate-900"
        if (variant === 'destructive') variantStyles = "bg-red-500 text-white hover:bg-red-600 shadow-sm shadow-red-200"

        let sizeStyles = "h-[30px] px-4 py-1"
        if (size === 'sm') sizeStyles = "h-9 rounded-md px-3"
        if (size === 'lg') sizeStyles = "h-11 rounded-md px-8"
        if (size === 'icon') sizeStyles = "h-[30px] w-[30px]"

        return (
            <motion.button
                whileHover={!disabled ? { scale: 1.01 } : {}}
                whileTap={!disabled ? { scale: 0.97 } : {}}
                transition={{ type: "spring", stiffness: 400, damping: 25 }}
                className={`${baseStyles} ${variantStyles} ${sizeStyles} ${className || ""}`}
                ref={ref}
                disabled={disabled}
                {...props}
            />
        )
    }
)
Button.displayName = "Button"

export { Button }
