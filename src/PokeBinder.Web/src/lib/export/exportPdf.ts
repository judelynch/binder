import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import type { BinderCardRow } from '../binder-cards-types'

export function exportBinderCardsToPdf(binderName: string, rows: BinderCardRow[]) {
  const doc = new jsPDF()

  doc.setFontSize(14)
  doc.text(`${binderName} — Card list`, 14, 16)
  doc.setFontSize(9)
  doc.setTextColor(120)
  doc.text(`${rows.length} card${rows.length === 1 ? '' : 's'} · generated ${new Date().toLocaleDateString()}`, 14, 22)

  autoTable(doc, {
    startY: 28,
    head: [['Card', 'Number', 'Set', 'Year', 'Have / Need', 'Tag']],
    body: rows.map((r) => [
      r.cardName,
      r.number,
      r.setName,
      r.releaseYear?.toString() ?? '—',
      r.owned ? 'Have' : 'Need',
      r.tagName ?? '—',
    ]),
    styles: { fontSize: 9 },
    headStyles: { fillColor: [90, 70, 40] },
    didParseCell: (data) => {
      if (data.section === 'body' && data.column.index === 4) {
        data.cell.styles.textColor = data.cell.raw === 'Have' ? [40, 120, 60] : [170, 60, 50]
        data.cell.styles.fontStyle = 'bold'
      }
    },
  })

  doc.save(`${binderName.replace(/[^a-z0-9]+/gi, '-').toLowerCase()}-card-list.pdf`)
}
