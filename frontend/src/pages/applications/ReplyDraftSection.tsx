import { useState } from 'react'
import type { SubmitEvent } from 'react'
import { api } from '../../api/client'
import { replyChannels, replyTones } from '../../api/types'
import type { ApplicationDetail, ReplyChannel, ReplyDraft, ReplyTone } from '../../api/types'
import { ErrorBanner } from '../../components/Feedback'
import { Field, Select } from '../../components/Field'
import { label, todayIso } from '../../labels'

interface Props {
  application: ApplicationDetail
  /** Reloads the page data after "Log as sent" adds a timeline entry. */
  onLogged: () => void
}

/**
 * Drafts a reply to a recruiter. The draft is only text: you edit it, copy it, and send it yourself.
 * Closed by default so the page stays calm; most visits to an application don't need a draft.
 */
export function ReplyDraftSection({ application, onLogged }: Props) {
  const [open, setOpen] = useState(false)
  const [channel, setChannel] = useState<ReplyChannel>('LinkedIn')
  const [tone, setTone] = useState<ReplyTone>('Short')
  const [contactId, setContactId] = useState(application.contacts[0]?.id ?? '')
  const [theirMessage, setTheirMessage] = useState('')
  const [intent, setIntent] = useState('')
  const [drafting, setDrafting] = useState(false)
  const [error, setError] = useState<unknown>()
  const [draft, setDraft] = useState<ReplyDraft>()
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [notice, setNotice] = useState('')

  const recipient = application.contacts.find((c) => c.id === contactId)

  const generate = async (e?: SubmitEvent) => {
    e?.preventDefault()
    setDrafting(true)
    setError(undefined)
    setNotice('')
    try {
      const result = await api.draftReply(application.id, {
        channel,
        tone,
        contactId: contactId || null,
        theirMessage: theirMessage.trim() || null,
        intent: intent.trim() || null,
      })
      setDraft(result)
      setSubject(result.subject ?? '')
      setBody(result.body)
    } catch (err) {
      setError(err)
    } finally {
      setDrafting(false)
    }
  }

  const copy = async () => {
    const text = channel === 'Email' && subject.trim() ? `Subject: ${subject.trim()}\n\n${body}` : body
    try {
      await navigator.clipboard.writeText(text)
      setNotice('✓ Copied. Paste it into LinkedIn or your email and send it from there.')
    } catch {
      setNotice('Could not copy automatically. Select the text and copy it with Ctrl+C.')
    }
  }

  const logAsSent = async () => {
    const summary = (channel === 'Email' && subject.trim() ? subject : body).replace(/\s+/g, ' ').trim()
    const text = `Sent ${channel === 'Email' ? 'email' : 'LinkedIn message'}${recipient ? ` to ${recipient.name}` : ''}: ${truncate(summary, 160)}`
    try {
      await api.addActivity(application.id, { type: 'MessageSent', text, date: todayIso() })
      setNotice('✓ Logged on the timeline as sent today.')
      onLogged()
    } catch (err) {
      setError(err)
    }
  }

  if (!open) {
    return (
      <section className="detail-section">
        <h2>Reply</h2>
        <p className="muted reply-intro">
          Draft a LinkedIn message or email to the recruiter with AI. You edit it and send it yourself.{' '}
          <button type="button" className="button-link" onClick={() => setOpen(true)}>
            Draft a reply
          </button>
        </p>
      </section>
    )
  }

  return (
    <section className="detail-section reply" aria-busy={drafting}>
      <h2>Reply</h2>
      <ErrorBanner error={error} />

      <form className="reply-form" onSubmit={generate}>
        <div className="reply-options">
          <Field label="To">
            <select value={contactId} onChange={(e) => setContactId(e.target.value)} disabled={drafting}>
              <option value="">No specific person</option>
              {application.contacts.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} ({label(c.role)})
                </option>
              ))}
            </select>
          </Field>
          <Field label="Channel">
            <Select value={channel} options={replyChannels} onChange={(v) => v && setChannel(v)} formatOption={label} />
          </Field>
          <Field label="Length">
            <Select value={tone} options={replyTones} onChange={(v) => v && setTone(v)} formatOption={label} />
          </Field>
        </div>
        <Field label="Their message (optional)" wide>
          <textarea
            rows={4}
            value={theirMessage}
            onChange={(e) => setTheirMessage(e.target.value)}
            placeholder="Paste the recruiter's message to reply to it. The draft uses the same language."
            disabled={drafting}
          />
        </Field>
        <Field label="What you want to say (optional)" wide>
          <input
            value={intent}
            onChange={(e) => setIntent(e.target.value)}
            placeholder="Interested; ask about the salary range and the interview process"
            maxLength={2000}
            disabled={drafting}
          />
        </Field>
        <div className="next-action-buttons">
          <button type="submit" className="button button-primary" disabled={drafting}>
            {drafting ? 'Drafting…' : draft ? 'Draft again' : 'Draft reply'}
          </button>
          <button type="button" className="button-sm button-ghost" disabled={drafting} onClick={() => setOpen(false)}>
            Close
          </button>
          {drafting && <span className="muted">Usually 5–20 seconds.</span>}
        </div>
      </form>

      {draft && (
        <div className="reply-draft">
          {channel === 'Email' && (
            <Field label="Subject" wide>
              <input value={subject} onChange={(e) => setSubject(e.target.value)} />
            </Field>
          )}
          <Field label="Draft (edit before sending)" wide>
            <textarea
              rows={Math.min(16, Math.max(6, body.split('\n').length + 1))}
              value={body}
              onChange={(e) => setBody(e.target.value)}
            />
          </Field>
          {body.includes('[') && <p className="hint">Fill in the [bracketed] placeholders before sending.</p>}
          <div className="analysis-footer">
            <span className="muted">Drafted with {draft.model}. Nothing is sent by the app.</span>
            <span className="next-action-buttons">
              <button type="button" className="button-sm button-soft" onClick={() => void copy()}>
                Copy
              </button>
              <button type="button" className="button-sm button-ghost" onClick={() => void logAsSent()}>
                Log as sent
              </button>
            </span>
          </div>
          {notice && (
            <p className="reply-notice" aria-live="polite">
              {notice}
            </p>
          )}
        </div>
      )}
    </section>
  )
}

function truncate(text: string, max: number): string {
  return text.length <= max ? text : `${text.slice(0, max - 1)}…`
}
