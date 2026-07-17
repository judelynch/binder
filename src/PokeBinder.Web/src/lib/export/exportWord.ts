import { Document, HeadingLevel, Packer, Paragraph, Table, TableCell, TableRow, TextRun, WidthType } from 'docx'
import type { BinderCardRow } from '../binder-cards-types'

function textCell(text: string, bold = false) {
  return new TableCell({ children: [new Paragraph({ children: [new TextRun({ text, bold })] })] })
}

export async function exportBinderCardsToWord(binderName: string, rows: BinderCardRow[]) {
  const headerRow = new TableRow({
    children: ['Card', 'Number', 'Set', 'Year', 'Have / Need', 'Tag'].map((text) => textCell(text, true)),
  })

  const bodyRows = rows.map(
    (r) =>
      new TableRow({
        children: [
          textCell(r.cardName),
          textCell(r.number),
          textCell(r.setName),
          textCell(r.releaseYear?.toString() ?? '—'),
          textCell(r.owned ? 'Have' : 'Need'),
          textCell(r.tagName ?? '—'),
        ],
      }),
  )

  const table = new Table({
    width: { size: 100, type: WidthType.PERCENTAGE },
    rows: [headerRow, ...bodyRows],
  })

  const doc = new Document({
    sections: [
      {
        children: [
          new Paragraph({ text: `${binderName} — Card list`, heading: HeadingLevel.HEADING_1 }),
          new Paragraph({ text: `${rows.length} card${rows.length === 1 ? '' : 's'} · generated ${new Date().toLocaleDateString()}` }),
          new Paragraph({ text: '' }),
          table,
        ],
      },
    ],
  })

  const blob = await Packer.toBlob(doc)
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `${binderName.replace(/[^a-z0-9]+/gi, '-').toLowerCase()}-card-list.docx`
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
  URL.revokeObjectURL(url)
}
