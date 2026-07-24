import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, Switch, Row, Col, message, Spin } from 'antd';
import { ScheduleOutlined, PlusOutlined, MailOutlined, FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

interface ScheduledReport {
  id: string;
  reportName: string;
  cronExpression: string;
  recipients: string;
  format: string;
  nextRunTime?: string;
  isActive: boolean;
}

export const AutomatedReportsPage: React.FC = () => {
  const { t } = useTranslation();
  const [reports, setReports] = useState<ScheduledReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();

  const fetchReports = async () => {
    setLoading(true);
    try {
      const res = await api.get('/analytics/reports');
      setReports(res.data);
    } catch {
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReports();
  }, []);

  const handleSubmit = async (values: any) => {
    try {
      await api.post('/analytics/reports', values);
      message.success(t('arep.saved', 'Scheduled report configured.'));
      setIsModalOpen(false);
      form.resetFields();
      fetchReports();
    } catch {
      message.error(t('arep.saveFailed', 'Failed to configure scheduled report.'));
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <ScheduleOutlined style={{ marginRight: 8, color: '#13c2c2' }} />
              {t('arep.title', 'Automated Scheduled Reports')}
            </h2>
            <div style={{ color: 'var(--text-secondary)', marginTop: 4 }}>
              {t('arep.subtitle', 'Configure recurring email reports for KPIs, SLA breaches, and Service Desk performance.')}
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
            {t('arep.schedule', 'Schedule New Report')}
          </Button>
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={reports}
            rowKey="id"
            columns={[
              { title: t('arep.colName', 'Report Name'), dataIndex: 'reportName', render: (n: string) => <strong>{n}</strong> },
              { title: t('arep.colCron', 'Schedule (Cron)'), dataIndex: 'cronExpression', render: (c: string) => <Tag color="purple">{c}</Tag> },
              {
                title: t('arep.colFormat', 'Format'),
                dataIndex: 'format',
                render: (f: string) => f === 'PDF' ? <Tag icon={<FilePdfOutlined />} color="red">PDF</Tag> : <Tag icon={<FileExcelOutlined />} color="green">Excel</Tag>
              },
              {
                title: t('arep.colRecipients', 'Recipients'),
                dataIndex: 'recipients',
                render: (r: string) => <Tag icon={<MailOutlined />} color="blue">{t('arep.recipientsCount', '{n} Recipients').replace('{n}', String(r.split(',').length))}</Tag>
              },
              {
                title: t('arep.colNextRun', 'Next Run'),
                dataIndex: 'nextRunTime',
                render: (d: string) => d ? new Date(d).toLocaleString() : t('arep.na', 'N/A')
              },
              {
                title: t('arep.colStatus', 'Status'),
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">{t('arep.active', 'Active')}</Tag> : <Tag color="default">{t('arep.paused', 'Paused')}</Tag>
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title={t('arep.configureTitle', 'Configure Scheduled Report')}
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ format: 'PDF', isActive: true, cronExpression: '0 8 * * 1' }}>
          <Form.Item name="reportName" label={t('arep.reportTitle', 'Report Title')} rules={[{ required: true }]}>
            <Input placeholder={t('arep.reportTitlePlaceholder', 'e.g. Weekly Executive Service Desk Summary')} />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="cronExpression" label={t('arep.cronLabel', 'Schedule (Cron Expression)')} rules={[{ required: true }]}>
                <Input placeholder={t('arep.cronPlaceholder', 'e.g. 0 8 * * 1 (Every Monday at 8 AM)')} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="format" label={t('arep.exportFormat', 'Export Format')}>
                <Select options={[
                  { value: 'PDF', label: t('arep.pdfDoc', 'PDF Document') },
                  { value: 'Excel', label: t('arep.excelSheet', 'Excel Spreadsheet') }
                ]} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="recipients" label={t('arep.recipientsLabel', 'Recipients (Comma separated emails)')} rules={[{ required: true }]}>
            <Input.TextArea placeholder="ceo@company.com, it-director@company.com" />
          </Form.Item>
          <Form.Item name="isActive" label={t('arep.isActive', 'Is Active')} valuePropName="checked">
            <Switch />
          </Form.Item>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>{t('common.cancel', 'Cancel')}</Button>
            <Button type="primary" htmlType="submit">{t('arep.saveSchedule', 'Save Schedule')}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};
