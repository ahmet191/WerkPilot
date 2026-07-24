import React, { useState, useEffect } from 'react';
import { Card, Typography, Timeline, Tag, Spin, Button, Descriptions, Collapse, Alert } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeftOutlined, ClockCircleOutlined, CheckCircleOutlined, CloseCircleOutlined, InfoCircleOutlined, WarningOutlined } from '@ant-design/icons';
import { api } from '../../core/api';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { Panel } = Collapse;

interface SystemLogDto {
    id: string;
    timestamp: string;
    level: string;
    message: string;
    exception?: string;
    traceId: string;
    userId?: string;
    properties: Record<string, any>;
}

export const TraceDetailsPage: React.FC = () => {
    const { traceId } = useParams<{ traceId: string }>();
    const navigate = useNavigate();
    const [logs, setLogs] = useState<SystemLogDto[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const fetchTrace = async () => {
            if (!traceId) return;
            setLoading(true);
            try {
                const res = await api.get(`/admin/systemlogs/trace/${traceId}`);
                setLogs(res.data);
            } catch (err) {
                console.error('Failed to fetch trace details', err);
            } finally {
                setLoading(false);
            }
        };
        fetchTrace();
    }, [traceId]);

    if (loading) {
        return <div style={{ textAlign: 'center', padding: '50px' }}><Spin size="large" /></div>;
    }

    if (logs.length === 0) {
        return <Alert message="No trace found" description={`Could not find any logs for Trace ID: ${traceId}`} type="warning" showIcon />;
    }

    const getIconForLevel = (level: string) => {
        switch (level) {
            case 'Error':
            case 'Fatal':
                return <CloseCircleOutlined style={{ color: 'red', fontSize: '16px' }} />;
            case 'Warning':
                return <WarningOutlined style={{ color: 'orange', fontSize: '16px' }} />;
            case 'Information':
                return <InfoCircleOutlined style={{ color: '#1890ff', fontSize: '16px' }} />;
            default:
                return <CheckCircleOutlined style={{ color: 'gray', fontSize: '16px' }} />;
        }
    };

    const getColorForLevel = (level: string) => {
        switch (level) {
            case 'Error':
            case 'Fatal':
                return 'red';
            case 'Warning':
                return 'orange';
            case 'Information':
                return 'blue';
            default:
                return 'default';
        }
    };

    const startTime = dayjs(logs[0].timestamp);
    const endTime = dayjs(logs[logs.length - 1].timestamp);
    const duration = endTime.diff(startTime, 'millisecond');

    return (
        <div style={{ maxWidth: 1000, margin: '0 auto' }}>
            <Button type="link" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)} style={{ marginBottom: 16 }}>
                Back to System Logs
            </Button>
            
            <Card style={{ marginBottom: 24 }}>
                <Title level={4}>Trace Investigation</Title>
                <Descriptions bordered size="small" column={2}>
                    <Descriptions.Item label="Trace ID" span={2}><Text copyable>{traceId}</Text></Descriptions.Item>
                    <Descriptions.Item label="Started At">{startTime.format('YYYY-MM-DD HH:mm:ss.SSS')}</Descriptions.Item>
                    <Descriptions.Item label="Duration">{duration} ms</Descriptions.Item>
                    <Descriptions.Item label="Total Steps">{logs.length}</Descriptions.Item>
                    <Descriptions.Item label="User ID">{logs[0].userId || 'Anonymous / System'}</Descriptions.Item>
                </Descriptions>
            </Card>

            <Card title="Request Lifecycle" bodyStyle={{ padding: '24px 24px 0 24px' }}>
                <Timeline mode="left">
                    {logs.map((log, index) => {
                        const timeOffset = index === 0 ? 0 : dayjs(log.timestamp).diff(startTime, 'millisecond');
                        
                        return (
                            <Timeline.Item 
                                key={log.id} 
                                dot={getIconForLevel(log.level)}
                                label={<Text type="secondary">+{timeOffset} ms</Text>}
                            >
                                <div style={{ marginBottom: 8 }}>
                                    <Tag color={getColorForLevel(log.level)}>{log.level.toUpperCase()}</Tag>
                                    <Text strong>{log.message}</Text>
                                </div>
                                
                                {log.exception && (
                                    <div style={{ marginTop: 8, marginBottom: 8 }}>
                                        <Alert 
                                            type="error" 
                                            message="Exception Thrown"
                                            description={
                                                <pre style={{ fontSize: '12px', whiteSpace: 'pre-wrap', maxHeight: 300, overflowY: 'auto' }}>
                                                    {log.exception}
                                                </pre>
                                            }
                                        />
                                    </div>
                                )}

                                {Object.keys(log.properties).length > 0 && (
                                    <Collapse ghost size="small">
                                        <Panel header="View Context Properties" key="1">
                                            <pre style={{ fontSize: '12px', backgroundColor: 'var(--bg-elevated)', padding: 8, borderRadius: 4 }}>
                                                {JSON.stringify(log.properties, null, 2)}
                                            </pre>
                                        </Panel>
                                    </Collapse>
                                )}
                            </Timeline.Item>
                        );
                    })}
                    
                    <Timeline.Item dot={<ClockCircleOutlined style={{ fontSize: '16px' }} />} label={<Text strong>{duration} ms</Text>}>
                        <Text strong>End of Trace</Text>
                    </Timeline.Item>
                </Timeline>
            </Card>
        </div>
    );
};
