import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert, Button, Card, Input, List, message, Modal, Space, Steps, Tag, Typography,
} from 'antd';
import {
  SafetyCertificateOutlined, QrcodeOutlined, KeyOutlined, CopyOutlined, WarningOutlined,
} from '@ant-design/icons';
import { QRCodeSVG } from 'qrcode.react';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text, Paragraph } = Typography;

interface MfaStatus {
  enabled: boolean;
  recoveryCodesLeft: number;
}

interface MfaSetup {
  sharedKey: string;
  otpauthUri: string;
}

/** Account security page: authenticator-app MFA enrollment and management. */
export const SecuritySettings: React.FC = () => {
  const { t } = useTranslation();
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [setup, setSetup] = useState<MfaSetup | null>(null);
  const [enableCode, setEnableCode] = useState('');
  const [disableCode, setDisableCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);
  const [busy, setBusy] = useState(false);

  const loadStatus = useCallback(async () => {
    try {
      const res = await api.get('/account/mfa/status');
      setStatus(res.data);
    } catch {
      // toast handled globally
    }
  }, []);

  useEffect(() => { loadStatus(); }, [loadStatus]);

  const beginSetup = async () => {
    setBusy(true);
    try {
      const res = await api.post('/account/mfa/setup');
      setSetup(res.data);
      setEnableCode('');
    } catch {
      // toast handled globally
    } finally {
      setBusy(false);
    }
  };

  const enable = async () => {
    setBusy(true);
    try {
      const res = await api.post('/account/mfa/enable', { code: enableCode });
      setRecoveryCodes(res.data.recoveryCodes ?? []);
      setSetup(null);
      await loadStatus();
      message.success(t('security.enabled', 'Multi-factor authentication is now enabled'));
    } catch {
      // toast handled globally
    } finally {
      setBusy(false);
    }
  };

  const disable = async () => {
    setBusy(true);
    try {
      await api.post('/account/mfa/disable', { code: disableCode });
      setDisableCode('');
      await loadStatus();
      message.success(t('security.disabled', 'Multi-factor authentication is disabled'));
    } catch {
      // toast handled globally
    } finally {
      setBusy(false);
    }
  };

  const copyRecoveryCodes = () => {
    navigator.clipboard.writeText((recoveryCodes ?? []).join('\n'));
    message.success(t('security.copied', 'Copied to clipboard'));
  };

  return (
    <div style={{ maxWidth: 720 }}>
      <Space align="center" style={{ marginBottom: 18 }}>
        <SafetyCertificateOutlined style={{ fontSize: 22, color: 'var(--accent-color)' }} />
        <Title level={3} style={{ margin: 0 }}>{t('security.title', 'Security')}</Title>
      </Space>

      <Card
        className="card-lift"
        title={
          <Space>
            {t('security.mfa', 'Two-factor authentication (2FA)')}
            {status && (
              <Tag color={status.enabled ? 'green' : 'default'} style={{ borderRadius: 999 }}>
                {status.enabled ? t('security.on', 'Enabled') : t('security.off', 'Disabled')}
              </Tag>
            )}
          </Space>
        }
        loading={status === null}
      >
        {status && !status.enabled && !setup && (
          <>
            <Paragraph type="secondary">
              {t('security.explain',
                'Protect your account with one-time codes from an authenticator app (Microsoft Authenticator, Google Authenticator, 1Password…). After enabling, sign-in requires your password and a 6-digit code.')}
            </Paragraph>
            <Button type="primary" icon={<QrcodeOutlined />} onClick={beginSetup} loading={busy}>
              {t('security.enable', 'Enable 2FA')}
            </Button>
          </>
        )}

        {setup && (
          <>
            <Steps
              size="small"
              current={1}
              style={{ marginBottom: 20 }}
              items={[
                { title: t('security.stepApp', 'Install app') },
                { title: t('security.stepScan', 'Scan QR') },
                { title: t('security.stepVerify', 'Verify') },
              ]}
            />
            <Space size={24} align="start" wrap>
              <div style={{ background: '#fff', padding: 12, borderRadius: 12, lineHeight: 0 }}>
                <QRCodeSVG value={setup.otpauthUri} size={168} />
              </div>
              <Space direction="vertical" style={{ maxWidth: 380 }}>
                <Text type="secondary">
                  {t('security.scanHint', 'Scan the QR code with your authenticator app, or enter the key manually:')}
                </Text>
                <Text code copyable style={{ fontSize: 13 }}>{setup.sharedKey}</Text>
                <Input
                  prefix={<KeyOutlined />}
                  placeholder={t('security.enterCode', '6-digit code from the app')}
                  value={enableCode}
                  onChange={e => setEnableCode(e.target.value)}
                  maxLength={10}
                  style={{ width: 240 }}
                  onPressEnter={enable}
                />
                <Space>
                  <Button type="primary" onClick={enable} loading={busy} disabled={enableCode.length < 6}>
                    {t('security.confirmEnable', 'Verify & enable')}
                  </Button>
                  <Button onClick={() => setSetup(null)}>{t('common.cancel', 'Cancel')}</Button>
                </Space>
              </Space>
            </Space>
          </>
        )}

        {status?.enabled && (
          <Space direction="vertical" style={{ width: '100%' }} size={16}>
            <Alert
              type="info"
              showIcon
              message={t('security.recoveryLeft', 'Recovery codes remaining')}
              description={`${status.recoveryCodesLeft} / 10`}
            />
            <Card size="small" title={<Space><WarningOutlined /> {t('security.disableTitle', 'Disable 2FA')}</Space>}>
              <Paragraph type="secondary" style={{ marginBottom: 12 }}>
                {t('security.disableHint', 'Enter a current code from your app (or a recovery code) to turn 2FA off.')}
              </Paragraph>
              <Space>
                <Input
                  placeholder={t('auth.mfaCode', 'Verification code')}
                  value={disableCode}
                  onChange={e => setDisableCode(e.target.value)}
                  maxLength={16}
                  style={{ width: 220 }}
                />
                <Button danger onClick={disable} loading={busy} disabled={disableCode.length < 6}>
                  {t('security.disable', 'Disable')}
                </Button>
              </Space>
            </Card>
          </Space>
        )}
      </Card>

      <Modal
        open={recoveryCodes !== null}
        title={t('security.recoveryTitle', 'Save your recovery codes')}
        onCancel={() => setRecoveryCodes(null)}
        footer={[
          <Button key="copy" icon={<CopyOutlined />} onClick={copyRecoveryCodes}>
            {t('security.copyAll', 'Copy all')}
          </Button>,
          <Button key="done" type="primary" onClick={() => setRecoveryCodes(null)}>
            {t('security.saved', "I've saved them")}
          </Button>,
        ]}
      >
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message={t('security.recoveryWarn',
            'Each code works once. Store them somewhere safe — they are the only way in if you lose your authenticator.')}
        />
        <List
          size="small"
          dataSource={recoveryCodes ?? []}
          grid={{ column: 2 }}
          renderItem={code => <List.Item><Text code>{code}</Text></List.Item>}
        />
      </Modal>
    </div>
  );
};
