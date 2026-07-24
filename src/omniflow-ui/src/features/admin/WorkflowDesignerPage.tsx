import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ReactFlow, Background, Controls, MiniMap, addEdge, useNodesState, useEdgesState,
  MarkerType, type Node, type Edge, type Connection, type EdgeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import {
  Alert, Button, Form, Input, Modal, Select, Space, Spin, Tag, Typography, message,
} from 'antd';
import { ArrowLeftOutlined, SaveOutlined, DeleteOutlined, ApartmentOutlined } from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../../core/api';
import { useTranslation } from '../../core/LanguageContext';

const { Title, Text } = Typography;

interface WorkflowTransition {
  fromState: string;
  toState: string;
  trigger: string;
  requiredRole?: string | null;
  approvalPolicy?: { steps: { order: number; approverType: string; approverRole?: string | null }[] } | null;
}

interface WorkflowResponse {
  templateId: string;
  name: string;
  workflow: { transitions: WorkflowTransition[] };
  states: string[];
  roles: string[];
  approverRoles: string[];
}

// A sensible default canvas position for each fixed state.
const STATE_POSITIONS: Record<string, { x: number; y: number }> = {
  New: { x: 40, y: 200 },
  InProgress: { x: 320, y: 200 },
  WaitingForApproval: { x: 320, y: 30 },
  Resolved: { x: 620, y: 120 },
  Closed: { x: 900, y: 120 },
};

interface EdgeData {
  trigger: string;
  requiredRole?: string | null;
  approvalRoles?: string[];
  [key: string]: unknown;
}

/**
 * Visual state-machine editor for a ticket template's workflow. States are the
 * fixed TicketState nodes; the operator draws and edits the transitions between
 * them. Saving validates server-side, so a broken workflow can't be persisted.
 */
export const WorkflowDesignerPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const [meta, setMeta] = useState<WorkflowResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  // Transitions are held until the state nodes are in React Flow's store —
  // edges set before their source/target nodes exist are silently dropped.
  const [pendingTransitions, setPendingTransitions] = useState<WorkflowTransition[] | null>(null);

  // Transition editor modal state.
  const [editing, setEditing] = useState<{ edgeId?: string; source: string; target: string } | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      const res = await api.get<WorkflowResponse>(`/templates/${id}/workflow`);
      setMeta(res.data);

      setNodes(res.data.states.map(state => ({
        id: state,
        position: STATE_POSITIONS[state] ?? { x: Math.random() * 600, y: Math.random() * 300 },
        data: { label: state },
        style: {
          padding: '10px 14px', borderRadius: 10, fontWeight: 600, fontSize: 13,
          border: '1px solid var(--accent-color)', background: 'var(--bg-elevated)', color: 'var(--text-primary)',
        },
      })));

      setEdges([]);
      setPendingTransitions(res.data.workflow.transitions);
    } catch {
      // toast handled globally
    } finally {
      setLoading(false);
    }
  }, [id, setNodes, setEdges]);

  useEffect(() => { load(); }, [load]);

  // Once the state nodes exist in the store, materialize the transitions as edges.
  useEffect(() => {
    if (pendingTransitions && nodes.length > 0) {
      setEdges(pendingTransitions.map((tr, i) => transitionToEdge(tr, i)));
      setPendingTransitions(null);
    }
  }, [pendingTransitions, nodes.length, setEdges]);

  // Drawing an edge opens the transition editor for the new connection.
  const onConnect = useCallback((connection: Connection) => {
    if (connection.source === connection.target) {
      message.warning(t('workflow.noSelfLoop', 'A transition must go to a different state'));
      return;
    }
    setEditing({ source: connection.source!, target: connection.target! });
  }, [t]);

  const onEdgeClick: EdgeMouseHandler = useCallback((_e, edge) => {
    setEditing({ edgeId: edge.id, source: edge.source, target: edge.target });
  }, []);

  // Populate the modal form whenever it opens for an existing edge.
  useEffect(() => {
    if (!editing) return;
    const existing = editing.edgeId ? edges.find(e => e.id === editing.edgeId) : undefined;
    const data = existing?.data as EdgeData | undefined;
    form.setFieldsValue({
      trigger: data?.trigger ?? '',
      requiredRole: data?.requiredRole ?? undefined,
      approvalRoles: data?.approvalRoles ?? [],
    });
  }, [editing, edges, form]);

  const saveTransition = async () => {
    const values = await form.validateFields();
    if (!editing) return;
    const data: EdgeData = {
      trigger: values.trigger.trim(),
      requiredRole: values.requiredRole ?? null,
      approvalRoles: editing.target === 'WaitingForApproval' ? (values.approvalRoles ?? []) : [],
    };

    if (editing.edgeId) {
      setEdges(prev => prev.map(e => e.id === editing.edgeId ? { ...e, label: data.trigger, data } : e));
    } else {
      const edge: Edge = {
        id: `${editing.source}-${editing.target}-${data.trigger}`,
        source: editing.source,
        target: editing.target,
        label: data.trigger,
        data,
        markerEnd: { type: MarkerType.ArrowClosed },
        animated: editing.target === 'WaitingForApproval',
      };
      setEdges(prev => addEdge(edge, prev));
    }
    setEditing(null);
  };

  const deleteTransition = () => {
    if (editing?.edgeId) {
      setEdges(prev => prev.filter(e => e.id !== editing.edgeId));
    }
    setEditing(null);
  };

  const save = async () => {
    setSaving(true);
    setErrors([]);
    try {
      const transitions: WorkflowTransition[] = edges.map(e => {
        const data = e.data as EdgeData;
        return {
          fromState: e.source,
          toState: e.target,
          trigger: data.trigger,
          requiredRole: data.requiredRole || null,
          approvalPolicy: (data.approvalRoles && data.approvalRoles.length > 0)
            ? { steps: data.approvalRoles.map((role, i) => ({ order: i + 1, approverType: 'Role', approverRole: role })) }
            : null,
        };
      });
      await api.put(`/templates/${id}/workflow`, { transitions });
      message.success(t('workflow.saved', 'Workflow saved'));
      await load();
    } catch (err: any) {
      const fieldErrors = err.response?.data?.errors?.workflow as string[] | undefined;
      if (fieldErrors) setErrors(fieldErrors);
    } finally {
      setSaving(false);
    }
  };

  const roleOptions = useMemo(() => (meta?.roles ?? []).map(r => ({ value: r, label: r })), [meta]);
  const approverRoleOptions = useMemo(() => (meta?.approverRoles ?? []).map(r => ({ value: r, label: r })), [meta]);

  if (loading) return <div style={{ textAlign: 'center', padding: 80 }}><Spin size="large" /></div>;

  return (
    <div>
      <Space style={{ marginBottom: 12 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/templates')}>
          {t('workflow.backToTemplates', 'Back to templates')}
        </Button>
      </Space>

      <div className="grid-toolbar" style={{ borderRadius: 'var(--radius-md)', marginBottom: 14 }}>
        <Space>
          <ApartmentOutlined style={{ color: 'var(--accent-color)' }} />
          <Title level={4} style={{ margin: 0 }}>{t('workflow.title', 'Workflow designer')}</Title>
          {meta && <Tag color="blue">{meta.name}</Tag>}
        </Space>
        <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>
          {t('common.save', 'Save')}
        </Button>
      </div>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 14 }}
        message={t('workflow.help', 'Drag from one state to another to add a transition. Click a transition to edit or delete it.')}
      />
      {errors.length > 0 && (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 14 }}
          message={t('workflow.invalid', 'The workflow could not be saved')}
          description={<ul style={{ margin: 0, paddingInlineStart: 18 }}>{errors.map((e, i) => <li key={i}>{e}</li>)}</ul>}
        />
      )}

      <div style={{ height: 560, border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', overflow: 'hidden' }}>
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          onEdgeClick={onEdgeClick}
          fitView
          proOptions={{ hideAttribution: true }}
        >
          <Background />
          <Controls />
          <MiniMap pannable zoomable />
        </ReactFlow>
      </div>

      <Modal
        open={editing !== null}
        title={editing ? `${editing.source} → ${editing.target}` : ''}
        onCancel={() => setEditing(null)}
        onOk={saveTransition}
        okText={t('common.save', 'Save')}
        destroyOnHidden
        footer={[
          ...(editing?.edgeId ? [<Button key="del" danger icon={<DeleteOutlined />} onClick={deleteTransition}>{t('common.delete', 'Delete')}</Button>] : []),
          <Button key="cancel" onClick={() => setEditing(null)}>{t('common.cancel', 'Cancel')}</Button>,
          <Button key="ok" type="primary" onClick={saveTransition}>{t('common.save', 'Save')}</Button>,
        ]}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="trigger"
            label={t('workflow.trigger', 'Trigger name')}
            rules={[{ required: true, message: t('workflow.triggerRequired', 'A trigger name is required') }]}
            extra={t('workflow.triggerHint', 'The action name shown as a button on the ticket (e.g. StartWork).')}
          >
            <Input placeholder="StartWork" />
          </Form.Item>
          <Form.Item name="requiredRole" label={t('workflow.requiredRole', 'Required role (optional)')}>
            <Select allowClear options={roleOptions} placeholder={t('workflow.anyRole', 'Anyone can trigger')} />
          </Form.Item>
          {editing?.target === 'WaitingForApproval' && (
            <Form.Item
              name="approvalRoles"
              label={t('workflow.approvalRoles', 'Approval steps (in order)')}
              extra={t('workflow.approvalHint', 'Each selected role becomes a sequential approval step.')}
            >
              <Select mode="multiple" options={approverRoleOptions} placeholder={t('workflow.selectApprovers', 'Select approver roles')} />
            </Form.Item>
          )}
          {editing && <Text type="secondary" style={{ fontSize: 12 }}>{editing.source} → {editing.target}</Text>}
        </Form>
      </Modal>
    </div>
  );
};

function transitionToEdge(tr: WorkflowTransition, index: number): Edge {
  const approvalRoles = (tr.approvalPolicy?.steps ?? [])
    .sort((a, b) => a.order - b.order)
    .map(s => s.approverRole)
    .filter((r): r is string => !!r);

  return {
    id: `${tr.fromState}-${tr.toState}-${tr.trigger || index}`,
    source: tr.fromState,
    target: tr.toState,
    label: tr.trigger,
    data: { trigger: tr.trigger, requiredRole: tr.requiredRole ?? null, approvalRoles },
    markerEnd: { type: MarkerType.ArrowClosed },
    animated: tr.toState === 'WaitingForApproval',
  };
}
