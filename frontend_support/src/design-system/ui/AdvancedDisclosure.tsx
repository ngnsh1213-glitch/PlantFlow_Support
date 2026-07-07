import { useId, useEffect, useRef, useState, type ReactNode } from 'react'
import { Settings2, ChevronDown } from 'lucide-react'

interface AdvancedDisclosureProps {
    children: ReactNode;
    label?: string;
}

// 모듈스코프 순수 래퍼. 비즈니스 로직 0. 닫힘 시 자식 언마운트 없이 grid 0fr/1fr로 접고,
// React 18은 inert JSX prop 미지원 -> ref로 DOM inert 설정해 Tab/SR 포커스 차단.
export function AdvancedDisclosure({ children, label = 'Advanced' }: AdvancedDisclosureProps) {
    const [open, setOpen] = useState(false);
    const contentRef = useRef<HTMLDivElement>(null);
    const contentId = useId();

    useEffect(() => {
        const el = contentRef.current;
        if (el) (el as any).inert = !open;
    }, [open]);

    return (
        <div className="pt-2 mt-1 border-t">
            <button
                type="button"
                onClick={() => setOpen(o => !o)}
                aria-expanded={open}
                aria-controls={contentId}
                aria-label="Advanced settings"
                className="flex items-center gap-1.5 w-full text-[11px] font-medium text-slate-400 hover:text-slate-600 transition-colors px-1 py-0.5"
            >
                <Settings2 size={12} />
                <span>{label}</span>
                <ChevronDown size={12} className={`ml-auto transition-transform duration-150 ${open ? 'rotate-180' : ''}`} />
            </button>
            <div
                className="grid transition-[grid-template-rows] duration-150 ease-out"
                style={{ gridTemplateRows: open ? '1fr' : '0fr' }}
            >
                <div ref={contentRef} id={contentId} aria-hidden={!open} className="min-h-0 overflow-hidden">
                    <div className="space-y-2 pt-2">
                        {children}
                    </div>
                </div>
            </div>
        </div>
    );
}

export default AdvancedDisclosure;
