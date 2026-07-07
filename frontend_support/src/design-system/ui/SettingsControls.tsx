import * as React from "react"
import { createPortal } from "react-dom"

const TOOLTIP_WIDTH = 256 // w-64 (16rem)

export function TooltipLabel({ label, helpText, className }: { label: string; helpText?: string; className?: string }) {
    const ref = React.useRef<HTMLSpanElement>(null)
    const [pos, setPos] = React.useState<{ top: number; left: number } | null>(null)

    const show = () => {
        const el = ref.current
        if (!el) return
        const r = el.getBoundingClientRect()
        // 뷰포트 우측 넘침 방지 클램프 (좌측 최소 8px 여백)
        const left = Math.max(8, Math.min(r.left, window.innerWidth - TOOLTIP_WIDTH - 12))
        setPos({ top: r.bottom + 6, left })
    }
    const hide = () => setPos(null)

    return (
        <span
            ref={ref}
            onMouseEnter={helpText ? show : undefined}
            onMouseLeave={helpText ? hide : undefined}
            className={`relative inline-flex items-center whitespace-nowrap ${helpText ? 'cursor-help' : ''} ${className || ''}`}
        >
            {label}
            {helpText && pos && createPortal(
                <span
                    className="pointer-events-none fixed z-[9999] w-64 whitespace-normal rounded-md border border-slate-200 bg-white px-3 py-2 text-[12px] font-medium leading-snug text-slate-600 shadow-lg"
                    style={{ top: pos.top, left: pos.left }}
                >
                    {helpText}
                </span>,
                document.body
            )}
        </span>
    )
}

type SettingsFieldVariant = 'split' | 'right'

export function SettingsField({
    label,
    children,
    helpText,
    variant = 'split',
}: {
    label: string
    children: React.ReactNode
    helpText?: string
    variant?: SettingsFieldVariant
}) {
    if (variant === 'right') {
        return (
            <div className="flex items-center gap-4">
                <label className="w-1/3 text-sm font-semibold text-slate-800 text-right">{label}</label>
                <div className="flex-1 max-w-[200px] flex items-center">
                    {children}
                </div>
            </div>
        )
    }

    return (
        <div className="flex items-center justify-between gap-4">
            <span className="text-sm font-medium text-slate-700 w-1/2 whitespace-nowrap">
                <TooltipLabel label={label} helpText={helpText} />
            </span>
            <div className="flex-1 max-w-[200px]">{children}</div>
        </div>
    )
}

type SettingsSelectVariant = 'soft' | 'plain'

export function SettingsSelect({
    value,
    options,
    disabled = false,
    variant = 'soft',
    onChange,
}: {
    value: string
    options: string[]
    disabled?: boolean
    variant?: SettingsSelectVariant
    onChange: (value: string) => void
}) {
    const softClass = "flex h-[30px] w-full rounded-xl border border-slate-200/80 bg-white/60 backdrop-blur-md px-3 py-1 text-sm ring-offset-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/30 transition-all shadow-sm appearance-none"
    const plainClass = `flex h-[30px] w-full rounded-xl border border-input px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 appearance-none transition-colors ${disabled ? 'bg-slate-100 text-slate-400 cursor-not-allowed' : 'bg-transparent text-slate-800 cursor-pointer'}`

    return (
        <select
            value={value}
            onChange={(e) => onChange(e.target.value)}
            disabled={disabled}
            className={variant === 'plain' ? plainClass : softClass}
        >
            {options.map(option => <option key={option} value={option}>{option}</option>)}
        </select>
    )
}
