import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import ProtectedRoute from '../components/ProtectedRoute';
import { AuthProvider } from '../auth/AuthContext';

// Mock token storage
vi.mock('../auth/tokenStorage', () => ({
  getAccessToken: () => 'mock-token',
  saveAccessToken: vi.fn(),
  clearAccessToken: vi.fn()
}));

vi.mock('../auth/jwt', () => ({
  extractRole: () => 'Admin',
  extractName: () => 'Тестовый пользователь'
}));

const MockAuthProvider = ({ children }: { children: React.ReactNode }) => (
  <AuthProvider>
    {children}
  </AuthProvider>
);

describe('ProtectedRoute', () => {
  it('renders children when user is authenticated', () => {
    render(
      <MockAuthProvider>
        <ProtectedRoute>
          <div data-testid="protected-content">Защищенный контент</div>
        </ProtectedRoute>
      </MockAuthProvider>
    );

    expect(screen.getByTestId('protected-content')).toBeInTheDocument();
  });

  it('renders children when user has required role', () => {
    render(
      <MockAuthProvider>
        <ProtectedRoute roles={['Admin']}>
          <div data-testid="protected-content">Контент для администратора</div>
        </ProtectedRoute>
      </MockAuthProvider>
    );

    expect(screen.getByTestId('protected-content')).toBeInTheDocument();
  });

  it('renders children when user has one of required roles', () => {
    render(
      <MockAuthProvider>
        <ProtectedRoute roles={['Admin', 'Engineer']}>
          <div data-testid="protected-content">Контент для нескольких ролей</div>
        </ProtectedRoute>
      </MockAuthProvider>
    );

    expect(screen.getByTestId('protected-content')).toBeInTheDocument();
  });
});
