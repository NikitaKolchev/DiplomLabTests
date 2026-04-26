import { describe, it, expect } from 'vitest';
import { parseJwt, extractRole, extractName } from '../auth/jwt';

describe('JWT Utilities', () => {
  const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6ItCi0LXRgdGC0L7QstGL0Lkg0L_QvtC70YzQt9C-0LLQsNGC0LXQu9GMIiwicm9sZSI6IkFkbWluIiwiZXhwIjo5OTk5OTk5OTk5fQ.test-signature';

  describe('parseJwt', () => {
    it('parses valid JWT token', () => {
      const payload = parseJwt(mockToken);
      
      expect(payload).not.toBeNull();
      expect(payload?.sub).toBe('1');
      expect(payload?.name).toBe('Тестовый пользователь');
      expect(payload?.role).toBe('Admin');
    });

    it('returns null for invalid token', () => {
      const payload = parseJwt('invalid.token.here');
      expect(payload).toBeNull();
    });

    it('returns null for empty string', () => {
      const payload = parseJwt('');
      expect(payload).toBeNull();
    });
  });

  describe('extractRole', () => {
    it('extracts role from valid token', () => {
      const role = extractRole(mockToken);
      expect(role).toBe('Admin');
    });

    it('returns null for invalid token', () => {
      const role = extractRole('invalid');
      expect(role).toBeNull();
    });
  });

  describe('extractName', () => {
    it('extracts name from valid token', () => {
      const name = extractName(mockToken);
      expect(name).toBe('Тестовый пользователь');
    });

    it('returns null for invalid token', () => {
      const name = extractName('invalid');
      expect(name).toBeNull();
    });
  });
});
