import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { CreateBinderForm, MAX_PAGES } from './CreateBinderForm'

describe('CreateBinderForm', () => {
  it('rejects submission with an empty name', async () => {
    const onSubmit = vi.fn()
    const user = userEvent.setup()
    render(<CreateBinderForm onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: /create binder/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Name is required.')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('rejects a page count above the maximum', async () => {
    const onSubmit = vi.fn()
    const user = userEvent.setup()
    render(<CreateBinderForm onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText(/^name$/i), 'Base Set Masters')

    const pageCountInput = screen.getByLabelText(/starting page count/i)
    await user.clear(pageCountInput)
    await user.type(pageCountInput, String(MAX_PAGES + 10))
    await user.click(screen.getByRole('button', { name: /create binder/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/between 0 and 60/i)
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('rejects an odd page count', async () => {
    const onSubmit = vi.fn()
    const user = userEvent.setup()
    render(<CreateBinderForm onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText(/^name$/i), 'Base Set Masters')

    const pageCountInput = screen.getByLabelText(/starting page count/i)
    await user.clear(pageCountInput)
    await user.type(pageCountInput, '5')
    await user.click(screen.getByRole('button', { name: /create binder/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/must be even/i)
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits with the entered name, chosen layout, and default page count when valid', async () => {
    const onSubmit = vi.fn()
    const user = userEvent.setup()
    render(<CreateBinderForm onSubmit={onSubmit} onCancel={vi.fn()} />)

    await user.type(screen.getByLabelText(/^name$/i), '  Base Set Masters  ')
    await user.click(screen.getByRole('button', { name: '3×3' }))
    await user.click(screen.getByRole('button', { name: /create binder/i }))

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'Base Set Masters', rows: 3, columns: 3, initialPageCount: 4 }),
    )
  })

  it('calls onCancel when the cancel button is clicked', async () => {
    const onCancel = vi.fn()
    const user = userEvent.setup()
    render(<CreateBinderForm onSubmit={vi.fn()} onCancel={onCancel} />)

    await user.click(screen.getByRole('button', { name: /cancel/i }))
    expect(onCancel).toHaveBeenCalledTimes(1)
  })
})
