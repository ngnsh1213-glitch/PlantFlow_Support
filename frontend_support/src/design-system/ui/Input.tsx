import * as React from "react"

const Input = React.forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement>>(
    ({ className, type, ...props }, ref) => {
        return (
            <input
                type={type}
                className={`flex h-[30px] w-full rounded-xl border border-slate-200/80 bg-white/60 backdrop-blur-md px-3 py-1 text-sm ring-offset-white file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-slate-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/30 focus-visible:border-blue-500 hover:bg-white transition-all duration-300 shadow-[0_2px_10px_-3px_rgba(0,0,0,0.03)] disabled:cursor-not-allowed disabled:opacity-50 ${className || ""}`}
                ref={ref}
                {...props}
            />
        )
    }
)
Input.displayName = "Input"

export { Input }
