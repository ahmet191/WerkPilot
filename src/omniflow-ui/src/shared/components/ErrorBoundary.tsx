import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import { Result, Button } from 'antd';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

/**
 * Top-level safety net: catches render/runtime errors anywhere in the tree and
 * shows a recoverable fallback instead of a blank white screen.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error:', error, info.componentStack);
  }

  private handleReload = () => {
    this.setState({ hasError: false, error: undefined });
    window.location.assign('/');
  };

  render() {
    if (this.state.hasError) {
      return (
        <Result
          status="error"
          title="Something went wrong"
          subTitle={this.state.error?.message ?? 'An unexpected error occurred while rendering this page.'}
          extra={<Button type="primary" onClick={this.handleReload}>Back to dashboard</Button>}
          style={{ marginTop: 80 }}
        />
      );
    }
    return this.props.children;
  }
}
