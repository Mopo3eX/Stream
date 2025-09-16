using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Stream.Classes.Maps;
namespace Stream.Helpers
{
    public class NHibernateHelper
    {
        // Синглтон для фабрики сессий
        private static ISessionFactory _sessionFactory;

        // Метод для создания или получения существующей фабрики сессий
        public static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    // Настройка подключения к MySQL с помощью Fluent NHibernate
                    _sessionFactory = Fluently.Configure()
                        .Database(MySQLConfiguration.Standard
                            .ConnectionString(c => c
                                .Server("127.0.0.1")      // Адрес сервера MySQL
                                .Database("newstream")         // Имя базы данных
                                .Username("stream")           // Имя пользователя для подключения
                                .Password("k1ufImZgjCaQ")       // Пароль для подключения
                            )
                            .ShowSql()                     // Вывод SQL-запросов в консоль (для отладки)
                        )
                        .Mappings(m =>
                        {
                            // Автоматическое сканирование сборки на наличие маппингов
                            m.FluentMappings.AddFromAssemblyOf<SerialMap>();
                        })
                        // Генерация схемы базы данных (создание/обновление таблиц) для примера.
                        .ExposeConfiguration(cfg =>
                        {
                            // Для демонстрационных целей – схема будет воссоздаваться при каждом запуске.
                            //new SchemaExport(cfg).Create(true, true);
                            new SchemaUpdate(cfg).Execute(false, true);
                        })
                        .BuildSessionFactory();
                }
                return _sessionFactory;
            }
        }

        // Метод для открытия сессии работы с базой данных
        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }
    }
}
