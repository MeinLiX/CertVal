import type { Language } from '$lib/types';

export const translations = {
    uk: {
        common: {
            loading: 'Завантаження...',
            save: 'Зберегти',
            cancel: 'Скасувати',
            delete: 'Видалити',
            edit: 'Редагувати',
            close: 'Закрити',
            confirm: 'Підтвердити',
            back: 'Назад',
            next: 'Далі',
            search: 'Пошук',
            filter: 'Фільтр',
            export: 'Експорт',
            import: 'Імпорт',
            upload: 'Завантажити',
            download: 'Скачати',
            refresh: 'Оновити',
            language: 'Мова'
        },
        nav: {
            dashboard: 'Панель керування',
            workspaces: 'Робочі простори',
            certificates: 'Сертифікати',
            notifications: 'Сповіщення',
            settings: 'Налаштування',
            profile: 'Профіль',
            logout: 'Вихід',
            login: 'Вхід',
            register: 'Реєстрація'
        },
        auth: {
            login: {
                title: 'Вхід до CertVal',
                email: 'Електронна пошта',
                password: 'Пароль',
                submit: 'Увійти',
                forgot: 'Забули пароль?',
                noAccount: 'Немає акаунту?',
                registerLink: 'Зареєструватися'
            },
            register: {
                title: 'Реєстрація в CertVal',
                firstName: "Ім'я",
                lastName: 'Прізвище',
                email: 'Електронна пошта',
                password: 'Пароль',
                confirmPassword: 'Підтвердження пароля',
                submit: 'Зареєструватися',
                hasAccount: 'Вже є акаунт?',
                loginLink: 'Увійти'
            }
        },
        dashboard: {
            title: 'Панель керування',
            welcome: 'Ласкаво просимо до CertVal',
            stats: {
                totalWorkspaces: 'Всього робочих просторів',
                totalCertificates: 'Всього сертифікатів',
                expiredCertificates: 'Прострочені сертифікати',
                expiringIn7Days: 'Закінчуються за 7 днів',
                expiringIn30Days: 'Закінчуються за 30 днів',
                validCertificates: 'Дійсні сертифікати'
            },
            recentCertificates: 'Нещодавні сертифікати',
            expiringCertificates: 'Сертифікати, що закінчуються'
        },
        workspaces: {
            title: 'Робочі простори',
            create: 'Створити робочий простір',
            name: 'Назва',
            description: 'Опис',
            owner: 'Власник',
            certificates: 'Сертифікати',
            members: 'Учасники',
            settings: 'Налаштування',
            empty: 'Немає робочих просторів',
            createFirst: 'Створіть свій перший робочий простір'
        },
        certificates: {
            title: 'Сертифікати',
            upload: 'Завантажити сертифікат',
            subject: 'Суб`єкт',
            issuer: 'Видавець',
            expires: 'Закінчується',
            status: 'Статус',
            expired: 'Прострочений',
            expiring: 'Закінчується',
            valid: 'Дійсний',
            days: 'днів',
            empty: 'Немає сертифікатів',
            uploadFirst: 'Завантажте ваш перший сертифікат'
        },
        errors: {
            general: 'Виникла помилка. Спробуйте знову.',
            network: 'Помилка мережі. Перевірте з`єднання.',
            unauthorized: 'Необхідна авторизація',
            forbidden: 'Доступ заборонено',
            notFound: 'Не знайдено',
            validation: 'Помилка валідації'
        }
    },
    en: {
        common: {
            loading: 'Loading...',
            save: 'Save',
            cancel: 'Cancel',
            delete: 'Delete',
            edit: 'Edit',
            close: 'Close',
            confirm: 'Confirm',
            back: 'Back',
            next: 'Next',
            search: 'Search',
            filter: 'Filter',
            export: 'Export',
            import: 'Import',
            upload: 'Upload',
            download: 'Download',
            refresh: 'Refresh',
            language: 'Language'
        },
        nav: {
            dashboard: 'Dashboard',
            workspaces: 'Workspaces',
            certificates: 'Certificates',
            notifications: 'Notifications',
            settings: 'Settings',
            profile: 'Profile',
            logout: 'Logout',
            login: 'Login',
            register: 'Register'
        },
        auth: {
            login: {
                title: 'Login to CertVal',
                email: 'Email',
                password: 'Password',
                submit: 'Sign In',
                forgot: 'Forgot password?',
                noAccount: "Don't have an account?",
                registerLink: 'Sign up'
            },
            register: {
                title: 'Register for CertVal',
                firstName: 'First Name',
                lastName: 'Last Name',
                email: 'Email',
                password: 'Password',
                confirmPassword: 'Confirm Password',
                submit: 'Sign Up',
                hasAccount: 'Already have an account?',
                loginLink: 'Sign in'
            }
        },
        dashboard: {
            title: 'Dashboard',
            welcome: 'Welcome to CertVal',
            stats: {
                totalWorkspaces: 'Total Workspaces',
                totalCertificates: 'Total Certificates',
                expiredCertificates: 'Expired Certificates',
                expiringIn7Days: 'Expiring in 7 days',
                expiringIn30Days: 'Expiring in 30 days',
                validCertificates: 'Valid Certificates'
            },
            recentCertificates: 'Recent Certificates',
            expiringCertificates: 'Expiring Certificates'
        },
        workspaces: {
            title: 'Workspaces',
            create: 'Create Workspace',
            name: 'Name',
            description: 'Description',
            owner: 'Owner',
            certificates: 'Certificates',
            members: 'Members',
            settings: 'Settings',
            empty: 'No workspaces',
            createFirst: 'Create your first workspace'
        },
        certificates: {
            title: 'Certificates',
            upload: 'Upload Certificate',
            subject: 'Subject',
            issuer: 'Issuer',
            expires: 'Expires',
            status: 'Status',
            expired: 'Expired',
            expiring: 'Expiring',
            valid: 'Valid',
            days: 'days',
            empty: 'No certificates',
            uploadFirst: 'Upload your first certificate'
        },
        errors: {
            general: 'An error occurred. Please try again.',
            network: 'Network error. Check your connection.',
            unauthorized: 'Authorization required',
            forbidden: 'Access denied',
            notFound: 'Not found',
            validation: 'Validation error'
        }
    }
};

export function t(key: string, lang: Language = 'uk'): string {
    const keys = key.split('.');
    let value: any = translations[lang];

    for (const k of keys) {
        if (value && typeof value === 'object' && k in value) {
            value = value[k];
        } else {
            return key;
        }
    }

    return typeof value === 'string' ? value : key;
}