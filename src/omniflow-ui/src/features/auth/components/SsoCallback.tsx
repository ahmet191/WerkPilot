import React, { useEffect, useRef } from 'react';
import { Spin, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../../core/AuthContext';
import { useTranslation } from '../../../core/LanguageContext';

/**
 * Landing point for the corporate SSO round-trip. The API redirects here with
 * either "#ticket=..." (swap for tokens) or "#error=..." in the URL fragment.
 */
export const SsoCallback: React.FC = () => {
  const { loginWithSsoTicket } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const exchanged = useRef(false);

  useEffect(() => {
    if (exchanged.current) return; // StrictMode double-invoke guard: the ticket is single-use
    exchanged.current = true;

    const fragment = new URLSearchParams(window.location.hash.replace(/^#/, ''));
    const ticket = fragment.get('ticket');
    const error = fragment.get('error');

    // Scrub the ticket from the address bar/history immediately.
    window.history.replaceState(null, '', window.location.pathname);

    if (!ticket) {
      navigate(`/login?sso_error=${encodeURIComponent(error ?? 'missing_ticket')}`, { replace: true });
      return;
    }

    loginWithSsoTicket(ticket)
      .then(() => navigate('/', { replace: true }))
      .catch(() => navigate('/login?sso_error=exchange_failed', { replace: true }));
  }, [loginWithSsoTicket, navigate]);

  return (
    <div className="auth-bg" style={{ flexDirection: 'column', gap: 16 }}>
      <Spin size="large" />
      <Typography.Text type="secondary">
        {t('auth.ssoCompleting', 'Completing corporate sign-in…')}
      </Typography.Text>
    </div>
  );
};
