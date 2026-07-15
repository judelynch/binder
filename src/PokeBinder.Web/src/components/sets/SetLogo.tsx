import { useState } from 'react'

/** Set logos are wide wordmarks, not card-shaped art — object-contain, never CardImage's object-cover. */
export function SetLogo({ src, alt }: { src: string | null; alt: string }) {
  const [failed, setFailed] = useState(false)

  if (!src || failed) {
    return <span className="px-3 text-center text-xs font-semibold text-ink-faint">{alt}</span>
  }

  return <img src={src} alt={alt} loading="lazy" onError={() => setFailed(true)} className="max-h-full max-w-full object-contain" />
}
