import React, { useEffect, useState } from 'react';
import { Card, Form, Input, Button, Typography, Alert, Space, Divider } from 'antd';
import { UserOutlined, LockOutlined, SafetyOutlined, BankOutlined } from '@ant-design/icons';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../../core/AuthContext';
import { useTranslation } from '../../../core/LanguageContext';
import { api } from '../../../core/api';

const { Title, Text } = Typography;

interface SsoProvider {
  id: string;
  displayName: string;
}

export const LoginPage: React.FC = () => {
  const { login } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mfaStep, setMfaStep] = useState(false);
  const [ssoProviders, setSsoProviders] = useState<SsoProvider[]>([]);

  const from = location.state?.from?.pathname || '/';
  const ssoError = searchParams.get('sso_error');

  useEffect(() => {
    api.get('/auth/sso/providers')
      .then(res => setSsoProviders(Array.isArray(res.data) ? res.data : []))
      .catch(() => setSsoProviders([]));
  }, []);

  const onFinish = async (values: any) => {
    try {
      setLoading(true);
      setError(null);
      await login(values.username, values.password, mfaStep ? values.mfaCode : undefined);
      navigate(from, { replace: true });
    } catch (err: any) {
      const data = err.response?.data;
      if (data?.error === 'mfa_required') {
        setMfaStep(true);
        return;
      }
      if (mfaStep && data?.error === 'invalid_grant') {
        setError(t('auth.mfaInvalid', 'The verification code is invalid.'));
        return;
      }
      console.error(err);
      setError(data?.error_description || 'Login failed. Please check your credentials.');
      setMfaStep(false);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-bg">
      <Card className="auth-card glass-panel" variant="borderless">
        <Space direction="vertical" style={{ width: '100%', textAlign: 'center', marginBottom: 24 }}>
          <span
            className="brand-mark"
            style={{ width: 52, height: 52, fontSize: 26, margin: '0 auto', borderRadius: 'var(--radius-md)' }}
          >
            W
          </span>
          <Title level={3} style={{ margin: '14px 0 0 0', letterSpacing: 0.2 }}>WerkPilot</Title>
          <Text type="secondary" style={{ letterSpacing: 0.3 }}>
            {mfaStep
              ? t('auth.mfaSubtitle', 'Enter the code from your authenticator app')
              : t('auth.subtitle', 'Sign in to your workspace')}
          </Text>
        </Space>

        {error && (
          <Alert
            message={t('auth.failed', 'Authentication Failed')}
            description={error}
            type="error"
            showIcon
            style={{ marginBottom: 24 }}
          />
        )}
        {ssoError && !error && (
          <Alert
            message={t('auth.ssoFailed', 'SSO sign-in failed')}
            description={t(`auth.ssoError.${ssoError}`, ssoError)}
            type="warning"
            showIcon
            style={{ marginBottom: 24 }}
          />
        )}

        <Form
          name="login_form"
          layout="vertical"
          initialValues={{ remember: true }}
          onFinish={onFinish}
          size="large"
        >
          <Form.Item
            name="username"
            hidden={mfaStep}
            rules={[{ required: true, message: 'Please input your email!' }]}
          >
            <Input prefix={<UserOutlined className="site-form-item-icon" />} placeholder="Email" />
          </Form.Item>

          <Form.Item
            name="password"
            hidden={mfaStep}
            rules={[{ required: true, message: 'Please input your password!' }]}
          >
            <Input.Password
              prefix={<LockOutlined className="site-form-item-icon" />}
              placeholder="Password"
            />
          </Form.Item>

          {mfaStep && (
            <Form.Item
              name="mfaCode"
              rules={[{ required: true, message: t('auth.mfaCodeRequired', 'Enter your verification code') }]}
            >
              <Input
                prefix={<SafetyOutlined className="site-form-item-icon" />}
                placeholder={t('auth.mfaCode', 'Verification code')}
                autoFocus
                autoComplete="one-time-code"
                maxLength={16}
              />
            </Form.Item>
          )}

          <Form.Item style={{ marginBottom: mfaStep ? 8 : undefined }}>
            <Button type="primary" htmlType="submit" style={{ width: '100%' }} loading={loading}>
              {mfaStep ? t('auth.verify', 'Verify') : t('auth.signin', 'Sign In')}
            </Button>
          </Form.Item>

          {mfaStep && (
            <Button type="link" block onClick={() => { setMfaStep(false); setError(null); }}>
              {t('auth.backToLogin', 'Back to sign in')}
            </Button>
          )}
        </Form>

        {!mfaStep && ssoProviders.length > 0 && (
          <>
            <Divider plain style={{ margin: '8px 0 16px' }}>
              <Text type="secondary" style={{ fontSize: 12 }}>{t('auth.or', 'or')}</Text>
            </Divider>
            <Space direction="vertical" style={{ width: '100%' }}>
              {ssoProviders.map(provider => (
                <Button
                  key={provider.id}
                  icon={<BankOutlined />}
                  block
                  size="large"
                  onClick={() => { window.location.href = '/api/auth/sso/challenge'; }}
                >
                  {t('auth.continueWith', 'Continue with')} {provider.displayName}
                </Button>
              ))}
            </Space>
          </>
        )}
      </Card>
    </div>
  );
};
