import React, { useMemo, useState } from 'react';
import { Alert, Button, Card, Form, Input, Space, Typography } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

const { Title, Text } = Typography;

/** Landing page for emailed reset links: /reset-password#email=..&token=.. */
export const ResetPassword: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // The token lives in the fragment so it never reaches server logs.
  const { email, token } = useMemo(() => {
    const fragment = new URLSearchParams(window.location.hash.replace(/^#/, ''));
    return { email: fragment.get('email') ?? '', token: fragment.get('token') ?? '' };
  }, []);

  const onFinish = async (values: { newPassword: string }) => {
    setLoading(true);
    setError(null);
    try {
      await api.post('/account/reset-password', { email, token, newPassword: values.newPassword });
      navigate('/login', { replace: true });
    } catch (err: any) {
      const errors = err.response?.data?.errors;
      const first = errors && (Object.values(errors)[0] as string[] | undefined)?.[0];
      setError(first ?? t('auth.resetFailed', 'The reset link is invalid or has expired.'));
    } finally {
      setLoading(false);
    }
  };

  const linkValid = email.length > 0 && token.length > 0;

  return (
    <div className="auth-bg">
      <Card className="auth-card glass-panel" variant="borderless">
        <Space direction="vertical" style={{ width: '100%', textAlign: 'center', marginBottom: 24 }}>
          <span className="brand-mark" style={{ width: 52, height: 52, fontSize: 26, margin: '0 auto', borderRadius: 'var(--radius-md)' }}>W</span>
          <Title level={3} style={{ margin: '14px 0 0 0' }}>{t('auth.resetTitle', 'Set a new password')}</Title>
          {linkValid && <Text type="secondary">{email}</Text>}
        </Space>

        {!linkValid ? (
          <Space direction="vertical" style={{ width: '100%' }} size={16}>
            <Alert type="error" showIcon message={t('auth.resetFailed', 'The reset link is invalid or has expired.')} />
            <Link to="/forgot-password"><Button block>{t('auth.requestNew', 'Request a new link')}</Button></Link>
          </Space>
        ) : (
          <>
            {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} />}
            <Form layout="vertical" onFinish={onFinish} size="large">
              <Form.Item
                name="newPassword"
                rules={[{ required: true, min: 8, message: t('auth.passwordMin', 'At least 8 characters') }]}
              >
                <Input.Password prefix={<LockOutlined />} placeholder={t('auth.newPassword', 'New password')} autoFocus autoComplete="new-password" />
              </Form.Item>
              <Form.Item
                name="confirm"
                dependencies={['newPassword']}
                rules={[
                  { required: true, message: t('auth.confirmRequired', 'Confirm the password') },
                  ({ getFieldValue }) => ({
                    validator: (_, value) =>
                      value === getFieldValue('newPassword')
                        ? Promise.resolve()
                        : Promise.reject(new Error(t('auth.passwordMismatch', 'Passwords do not match'))),
                  }),
                ]}
              >
                <Input.Password prefix={<LockOutlined />} placeholder={t('auth.confirmPassword', 'Confirm password')} autoComplete="new-password" />
              </Form.Item>
              <Form.Item>
                <Button type="primary" htmlType="submit" block loading={loading}>
                  {t('auth.resetSubmit', 'Update password')}
                </Button>
              </Form.Item>
            </Form>
          </>
        )}
      </Card>
    </div>
  );
};
