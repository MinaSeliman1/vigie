import { describe, expect, it } from 'vitest'
import { createShiftRequest, validateCreateShiftDraft, type CreateShiftDraft } from './createShift'

const validDraft: CreateShiftDraft = {
  siteId: '20000000-0000-0000-0000-000000000001',
  date: '2026-09-15',
  startTime: '09:00',
  endTime: '17:00',
  requiredLifeguards: 2,
}

describe('création de quart', () => {
  it('accepte un horaire dont la fin suit le début', () => {
    expect(validateCreateShiftDraft(validDraft)).toEqual({})
  })

  it('refuse une fin antérieure ou égale au début', () => {
    expect(validateCreateShiftDraft({ ...validDraft, endTime: '09:00' })).toEqual({
      endTime: 'La fin doit être après le début.',
    })
  })

  it('convertit le brouillon en requête API avec des instants ISO', () => {
    const expectedStart = new Date('2026-09-15T09:00:00').toISOString()
    const expectedEnd = new Date('2026-09-15T17:00:00').toISOString()

    expect(createShiftRequest(validDraft)).toEqual({
      siteId: validDraft.siteId,
      startUtc: expectedStart,
      endUtc: expectedEnd,
      requiredLifeguards: 2,
    })
  })
})
