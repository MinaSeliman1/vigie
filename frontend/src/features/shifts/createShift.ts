import type { CreateShiftInput } from '../../api/types'

export type CreateShiftDraft = {
  siteId: string
  date: string
  startTime: string
  endTime: string
  requiredLifeguards: number
}

export type CreateShiftErrors = Partial<Record<keyof CreateShiftDraft | 'form', string>>

function localDateTime(date: string, time: string) {
  return new Date(`${date}T${time}:00`)
}

export function validateCreateShiftDraft(draft: CreateShiftDraft): CreateShiftErrors {
  if (!draft.siteId) return { siteId: 'Choisissez un site.' }
  if (!draft.date) return { date: 'Choisissez une date.' }
  if (!draft.startTime || !draft.endTime) return { form: 'Indiquez une heure de début et de fin.' }

  const start = localDateTime(draft.date, draft.startTime)
  const end = localDateTime(draft.date, draft.endTime)
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return { form: 'La date ou l’heure est invalide.' }
  if (end <= start) return { endTime: 'La fin doit être après le début.' }
  if (!Number.isInteger(draft.requiredLifeguards) || draft.requiredLifeguards < 1 || draft.requiredLifeguards > 50) {
    return { requiredLifeguards: 'Le nombre doit être compris entre 1 et 50.' }
  }
  return {}
}

export function createShiftRequest(draft: CreateShiftDraft): CreateShiftInput {
  const errors = validateCreateShiftDraft(draft)
  if (Object.keys(errors).length > 0) throw new Error(Object.values(errors)[0])

  return {
    siteId: draft.siteId,
    startUtc: localDateTime(draft.date, draft.startTime).toISOString(),
    endUtc: localDateTime(draft.date, draft.endTime).toISOString(),
    requiredLifeguards: draft.requiredLifeguards,
  }
}
