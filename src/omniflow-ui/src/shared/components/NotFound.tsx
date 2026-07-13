import { Result, Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from '../../core/LanguageContext';

/** 404 fallback for unmatched routes. */
export function NotFound() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  return (
    <Result
      status="404"
      title="404"
      subTitle={t('error.notFound', 'Sorry, the page you visited does not exist.')}
      extra={
        <Button type="primary" onClick={() => navigate('/')}>
          {t('error.backHome', 'Back to dashboard')}
        </Button>
      }
      style={{ marginTop: 80 }}
    />
  );
}
