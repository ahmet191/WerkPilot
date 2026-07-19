import React, { useState } from 'react';
import { Card, Form, Input, Button, Typography, Alert, Space } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../../core/AuthContext';
import { useTranslation } from '../../../core/LanguageContext';

const { Title, Text } = Typography;

export const LoginPage: React.FC = () => {
  const { login } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const from = location.state?.from?.pathname || '/';

  const onFinish = async (values: any) => {
    try {
      setLoading(true);
      setError(null);
      await login(values.username, values.password);
      navigate(from, { replace: true });
    } catch (err: any) {
      console.error(err);
      setError(err.response?.data?.error_description || 'Login failed. Please check your credentials.');
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
            {t('auth.subtitle', 'Sign in to your workspace')}
          </Text>
        </Space>

        {error && (
          <Alert
            message="Authentication Failed"
            description={error}
            type="error"
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
            rules={[{ required: true, message: 'Please input your email!' }]}
          >
            <Input prefix={<UserOutlined className="site-form-item-icon" />} placeholder="Email" />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: 'Please input your password!' }]}
          >
            <Input.Password
              prefix={<LockOutlined className="site-form-item-icon" />}
              placeholder="Password"
            />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" style={{ width: '100%' }} loading={loading}>
              {t('auth.signin', 'Sign In')}
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};
