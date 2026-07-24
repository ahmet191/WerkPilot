import React, { useState, useEffect } from 'react';
import { Table, Tag, Input, Button, Select, Space, Card, Typography, Tooltip } from 'antd';
import { SearchOutlined, EyeOutlined, SyncOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';
import dayjs from 'dayjs';

const { Title, Text } = Typography;

interface SystemLogDto {
    id: string;
    timestamp: string;
    level: string;
    message: string;
    traceId?: string;
    userId?: string;
}

export const SystemLogsPage: React.FC = () => {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const [data, setData] = useState<SystemLogDto[]>([]);
    const [total, setTotal] = useState(0);
    const [loading, setLoading] = useState(false);

    // Pagination & Filters
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(50);
    const [traceId, setTraceId] = useState('');
    const [level, setLevel] = useState<string | null>(null);

    const fetchLogs = async () => {
        setLoading(true);
        try {
            const params = new URLSearchParams({
                pageIndex: (page - 1).toString(),
                pageSize: pageSize.toString(),
            });
            if (traceId) params.append('traceId', traceId);
            if (level) params.append('level', level);

            const res = await api.get(`/admin/systemlogs?${params.toString()}`);
            setData(res.data.items);
            setTotal(res.data.totalCount);
        } catch (err) {
            console.error('Failed to fetch logs', err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchLogs();
    }, [page, pageSize]);

    const handleSearch = () => {
        setPage(1);
        fetchLogs();
    };

    const columns = [
        {
            title: t('syslog.colTimestamp', 'Timestamp'),
            dataIndex: 'timestamp',
            key: 'timestamp',
            render: (val: string) => dayjs(val).format('YYYY-MM-DD HH:mm:ss.SSS'),
            width: 200,
        },
        {
            title: t('syslog.colLevel', 'Level'),
            dataIndex: 'level',
            key: 'level',
            render: (lvl: string) => {
                let color = 'blue';
                if (lvl === 'Error' || lvl === 'Fatal') color = 'red';
                if (lvl === 'Warning') color = 'orange';
                if (lvl === 'Debug') color = 'default';
                return <Tag color={color}>{lvl.toUpperCase()}</Tag>;
            },
            width: 100,
        },
        {
            title: t('syslog.colMessage', 'Message'),
            dataIndex: 'message',
            key: 'message',
            ellipsis: true,
        },
        {
            title: t('syslog.colTrace', 'Trace ID'),
            dataIndex: 'traceId',
            key: 'traceId',
            render: (val: string) => val ? <Text copyable>{val}</Text> : '-',
            width: 250,
        },
        {
            title: t('syslog.colAction', 'Action'),
            key: 'action',
            render: (_: any, record: SystemLogDto) => (
                <Tooltip title={t('syslog.viewTrace', 'View Request Trace')}>
                    <Button
                        type="primary"
                        icon={<EyeOutlined />}
                        size="small"
                        disabled={!record.traceId}
                        onClick={() => navigate(`/admin/logs/trace/${record.traceId}`)}
                    >
                        {t('syslog.trace', 'Trace')}
                    </Button>
                </Tooltip>
            ),
            width: 100,
        },
    ];

    return (
        <Card title={<Title level={4}>{t('syslog.title', 'System Logs')}</Title>} extra={<Button icon={<SyncOutlined />} onClick={fetchLogs}>{t('syslog.refresh', 'Refresh')}</Button>}>
            <Space style={{ marginBottom: 16 }}>
                <Select
                    placeholder={t('syslog.logLevel', 'Log Level')}
                    style={{ width: 120 }}
                    allowClear
                    value={level}
                    onChange={setLevel}
                >
                    <Select.Option value="Information">{t('syslog.info', 'Info')}</Select.Option>
                    <Select.Option value="Warning">{t('syslog.warning', 'Warning')}</Select.Option>
                    <Select.Option value="Error">{t('syslog.error', 'Error')}</Select.Option>
                    <Select.Option value="Fatal">{t('syslog.fatal', 'Fatal')}</Select.Option>
                    <Select.Option value="Debug">{t('syslog.debug', 'Debug')}</Select.Option>
                </Select>
                <Input
                    placeholder={t('syslog.searchTrace', 'Search by Trace ID')}
                    value={traceId}
                    onChange={e => setTraceId(e.target.value)}
                    onPressEnter={handleSearch}
                    style={{ width: 300 }}
                    prefix={<SearchOutlined />}
                    allowClear
                />
                <Button type="primary" onClick={handleSearch}>{t('syslog.search', 'Search')}</Button>
            </Space>

            <Table
                columns={columns}
                dataSource={data}
                rowKey="id"
                loading={loading}
                size="small"
                pagination={{
                    current: page,
                    pageSize: pageSize,
                    total: total,
                    showSizeChanger: true,
                    onChange: (p, s) => {
                        setPage(p);
                        setPageSize(s);
                    }
                }}
            />
        </Card>
    );
};
