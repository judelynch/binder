import { useEffect, useState } from 'react'

const BREAKPOINTS: { query: string; columns: number }[] = [
  { query: '(min-width: 1280px)', columns: 6 },
  { query: '(min-width: 1024px)', columns: 5 },
  { query: '(min-width: 768px)', columns: 4 },
  { query: '(min-width: 480px)', columns: 3 },
]

function resolveColumns(): number {
  for (const { query, columns } of BREAKPOINTS) {
    if (window.matchMedia(query).matches) return columns
  }
  return 2
}

export function useColumnCount(): number {
  const [columns, setColumns] = useState(resolveColumns)

  useEffect(() => {
    const mqls = BREAKPOINTS.map((b) => window.matchMedia(b.query))
    const onChange = () => setColumns(resolveColumns())
    mqls.forEach((mql) => mql.addEventListener('change', onChange))
    return () => mqls.forEach((mql) => mql.removeEventListener('change', onChange))
  }, [])

  return columns
}
