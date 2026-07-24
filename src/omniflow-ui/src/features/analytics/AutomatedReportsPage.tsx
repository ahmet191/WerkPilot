import React, { useEffect, useState } from 'react';
import { Card, Table, Tag, Button, Modal, Form, Input, Select, Switch, Row, Col, message, Spin } from 'antd';
import { ScheduleOutlined, PlusOutlined, MailOutlined, FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons';
import { api } from '../../core/api';

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
      message.success('Scheduled report configured successfully!');
      setIsModalOpen(false);
      form.resetFields();
      fetchReports();
    } catch {
      message.error('Failed to configure scheduled report.');
    }
  };

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Card style={{ borderRadius: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0, fontWeight: 700 }}>
              <ScheduleOutlined style={{ marginRight: 8, color: '#13c2c2' }} />
              Automated Scheduled Reports
            </h2>
            <div style={{ color: '#8c8c8c', marginTop: 4 }}>
              Configure recurring email reports for KPIs, SLA breaches, and Service Desk performance.
            </div>
          </div>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsModalOpen(true)}>
            Schedule New Report
          </Button>
        </div>

        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin size="large" /></div>
        ) : (
          <Table
            dataSource={reports}
            rowKey="id"
            columns={[
              { title: 'Report Name', dataIndex: 'reportName', render: (n: string) => <strong>{n}</strong> },
              { title: 'Schedule (Cron)', dataIndex: 'cronExpression', render: (c: string) => <Tag color="purple">{c}</Tag> },
              {
                title: 'Format',
                dataIndex: 'format',
                render: (f: string) => f === 'PDF' ? <Tag icon={<FilePdfOutlined />} color="red">PDF</Tag> : <Tag icon={<FileExcelOutlined />} color="green">Excel</Tag>
              },
              {
                title: 'Recipients',
                dataIndex: 'recipients',
                render: (r: string) => <Tag icon={<MailOutlined />} color="blue">{r.split(',').length} Recipients</Tag>
              },
              {
                title: 'Next Run',
                dataIndex: 'nextRunTime',
                render: (d: string) => d ? new Date(d).toLocaleString() : 'N/A'
              },
              {
                title: 'Status',
                dataIndex: 'isActive',
                render: (act: boolean) => act ? <Tag color="green">Active</Tag> : <Tag color="default">Paused</Tag>
              }
            ]}
          />
        )}
      </Card>

      <Modal
        title="Configure Scheduled Report"
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        footer={null}
      >
        <Form form={form} layout="vertical" onFinish={handleSubmit} initialValues={{ format: 'PDF', isActive: true, cronExpression: '0 8 * * 1' }}>
          <Form.Item name="reportName" label="Report Title" rules={[{ required: true }]}>
            <Input placeholder="e.g. Weekly Executive Service Desk Summary" />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="cronExpression" label="Schedule (Cron Expression)" rules={[{ required: true }]}>
                <Input placeholder="e.g. 0 8 * * 1 (Every Monday at 8 AM)" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="format" label="Export Format">
                <Select options={[
                  { value: 'PDF', label: 'PDF Document' },
                  { value: 'Excel', label: 'Excel Spreadsheet' }
                ]} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="recipients" label="Recipients (Comma separated emails)" rules={[{ required: true }]}>
            <Input.TextArea placeholder="ceo@company.com, it-director@company.com" />
          </Form.Item>
          <Form.Item name="isActive" label="Is Active" valuePropName="checked">
            <Switch />
          </Form.Item>
          <div style={{ textAlign: 'right', marginTop: 24 }}>
            <Button onClick={() => setIsModalOpen(false)} style={{ marginRight: 8 }}>Cancel</Button>
            <Button type="primary" htmlType="submit">Save Schedule</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};
