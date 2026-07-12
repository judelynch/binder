import { useState } from 'react'

export function CardImage({
  src,
  alt,
  greyscale,
}: {
  src: string | null
  alt: string
  greyscale: boolean
}) {
  const [failed, setFailed] = useState(false)

  if (!src || failed) {
    return (
      <div className="absolute inset-0 flex items-center justify-center bg-canvas/40">
        <span className="text-[9px] text-ink-faint">No image</span>
      </div>
    )
  }

  return (
    <img
      src={src}
      alt={alt}
      loading="lazy"
      onError={() => setFailed(true)}
      className={`absolute inset-0 h-full w-full object-cover transition-[filter] ${greyscale ? 'grayscale brightness-75' : ''}`}
    />
  )
}
