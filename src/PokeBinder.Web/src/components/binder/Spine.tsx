export function Spine() {
  return (
    <div className="relative hidden w-9 rounded shadow-[inset_0_0_12px_rgba(0,0,0,0.6)] sm:block" style={{ background: 'linear-gradient(90deg, #0e1712, #182620, #0e1712)' }}>
      {[14, 48, 82].map((top) => (
        <div
          key={top}
          className="absolute left-1/2 h-4 w-4 -translate-x-1/2 rounded-full shadow-[0_2px_4px_rgba(0,0,0,0.5)]"
          style={{ top: `${top}%`, background: 'radial-gradient(circle at 35% 30%, #e8c168, #a97a24 60%, #6f4f18)' }}
        />
      ))}
    </div>
  )
}
