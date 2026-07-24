import React, { useState, useEffect } from 'react';
import { Table, Tag, Input, Button, Select, Space, Card, Typography, Tooltip } from 'antd';
import { SearchOutlined, EyeOutlined, SyncOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { api } from '../../core/api';
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
            title: 'Timestamp',
            dataIndex: 'timestamp',
            key: 'timestamp',
            render: (val: string) => dayjs(val).format('YYYY-MM-DD HH:mm:ss.SSS'),
            width: 200,
        },
        {
            title: 'Level',
            dataIndex: 'level',
            key: 'level',
            render: (level: string) => {
                let color = 'blue';
                if (level === 'Error' || level === 'Fatal') color = 'red';
                if (level === 'Warning') color = 'orange';
                if (level === 'Debug') color = 'default';
                return <Tag color={color}>{level.toUpperCase()}</Tag>;
            },
            width: 100,
        },
        {
            title: 'Message',
            dataIndex: 'message',
            key: 'message',
            ellipsis: true,
        },
        {
            title: 'Trace ID',
            dataIndex: 'traceId',
            key: 'traceId',
            render: (val: string) => val ? <Text copyable>{val}</Text> : '-',
            width: 250,
        },
        {
            title: 'Action',
            key: 'action',
            render: (_: any, record: SystemLogDto) => (
                <Tooltip title="View Request Trace">
                    <Button 
                        type="primary" 
                        icon={<EyeOutlined />} 
                        size="small"
                        disabled={!record.traceId}
                        onClick={() => navigate(`/admin/logs/trace/${record.traceId}`)}
                    >
                        Trace
                    </Button>
                </Tooltip>
            ),
            width: 100,
        },
    ];

    return (
        <Card title={<Title level={4}>System Logs</Title>} extra={<Button icon={<SyncOutlined />} onClick={fetchLogs}>Refresh</Button>}>
            <Space style={{ marginBottom: 16 }}>
                <Select
                    placeholder="Log Level"
                    style={{ width: 120 }}
                    allowClear
                    value={level}
                    onChange={setLevel}
                >
                    <Select.Option value="Information">Info</Select.Option>
                    <Select.Option value="Warning">Warning</Select.Option>
                    <Select.Option value="Error">Error</Select.Option>
                    <Select.Option value="Fatal">Fatal</Select.Option>
                    <Select.Option value="Debug">Debug</Select.Option>
                </Select>
                <Input
                    placeholder="Search by Trace ID"
                    value={traceId}
                    onChange={e => setTraceId(e.target.value)}
                    onPressEnter={handleSearch}
                    style={{ width: 300 }}
                    prefix={<SearchOutlined />}
                    allowClear
                />
                <Button type="primary" onClick={handleSearch}>Search</Button>
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
