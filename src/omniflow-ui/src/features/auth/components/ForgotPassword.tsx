import React, { useState } from 'react';
import { Alert, Button, Card, Form, Input, Space, Typography } from 'antd';
import { MailOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import { api } from '../../../core/api';
import { useTranslation } from '../../../core/LanguageContext';

const { Title, Text } = Typography;

export const ForgotPassword: React.FC = () => {
  const { t } = useTranslation();
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: { email: string }) => {
    setLoading(true);
    try {
      await api.post('/account/forgot-password', { email: values.email });
      setSent(true);
    } catch {
      // Even rate-limit errors shouldn't reveal anything; show the generic state.
      setSent(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-bg">
      <Card className="auth-card glass-panel" variant="borderless">
        <Space direction="vertical" style={{ width: '100%', textAlign: 'center', marginBottom: 24 }}>
          <span className="brand-mark" style={{ width: 52, height: 52, fontSize: 26, margin: '0 auto', borderRadius: 'var(--radius-md)' }}>W</span>
          <Title level={3} style={{ margin: '14px 0 0 0' }}>{t('auth.forgotTitle', 'Forgot your password?')}</Title>
          <Text type="secondary">{t('auth.forgotSubtitle', 'Enter your email and we will send a reset link.')}</Text>
        </Space>

        {sent ? (
          <Space direction="vertical" style={{ width: '100%' }} size={16}>
            <Alert
              type="success"
              showIcon
              message={t('auth.forgotSent', 'Check your inbox')}
              description={t('auth.forgotSentHint', 'If the email is registered, a reset link is on its way.')}
            />
            <Link to="/login"><Button block>{t('auth.backToLogin', 'Back to sign in')}</Button></Link>
          </Space>
        ) : (
          <Form layout="vertical" onFinish={onFinish} size="large">
            <Form.Item name="email" rules={[{ required: true, type: 'email' }]}>
              <Input prefix={<MailOutlined />} placeholder="Email" autoFocus />
            </Form.Item>
            <Form.Item>
              <Button type="primary" htmlType="submit" block loading={loading}>
                {t('auth.sendReset', 'Send reset link')}
              </Button>
            </Form.Item>
            <Link to="/login">
              <Button type="link" block>{t('auth.backToLogin', 'Back to sign in')}</Button>
            </Link>
          </Form>
        )}
      </Card>
    </div>
  );
};
