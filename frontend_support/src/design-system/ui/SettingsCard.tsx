import * as React from "react"
import { Card, CardHeader, CardTitle } from './Card'

type SettingsCardVariant = 'default' | 'warning'

const cardVariantClass: Record<SettingsCardVariant, string> = {
    default: '!border-slate-200/70',
    warning: '!border-amber-200/80',
}

const headerVariantClass: Record<SettingsCardVariant, string> = {
    default: 'bg-slate-50/70 border-slate-200/70',
    warning: 'bg-amber-50/80 border-amber-200/70',
}

type SettingsCardProps = Omit<React.ComponentProps<typeof Card>, 'title'> & {
    title: React.ReactNode
    variant?: SettingsCardVariant
}

export function SettingsCard({ title, variant = 'default', className, children, ...props }: SettingsCardProps) {
    return (
        <Card
            className={`!rounded-[14px] ${cardVariantClass[variant]} !bg-white/70 !shadow-sm overflow-hidden ${className || ''}`}
            {...props}
        >
            <CardHeader className={`${headerVariantClass[variant]} border-b px-5 pt-3 pb-5`}>
                <CardTitle className="text-[17px] font-bold text-blue-600">{title}</CardTitle>
            </CardHeader>
            {children}
        </Card>
    )
}

export function SettingsSurface({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
    return (
        <div
            className={`rounded-[14px] border border-slate-200/70 bg-white/70 shadow-sm ${className || ''}`}
            {...props}
        />
    )
}
