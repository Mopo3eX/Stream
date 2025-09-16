using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stream.Classes;
using Stream.Helpers;

namespace Stream
{


    public class WeeklyScheduleGenerator
    {
        /// <summary>
        /// Генерирует расписание на неделю вперёд, начиная с указанной даты.
        /// Для каждого сериала эпизоды транслируются последовательно, выбирается первая серия, у которой State != "Scheduled".
        /// После добавления в расписание состояние эпизода меняется на "Scheduled".
        /// </summary>
        /// <param name="startDate">Дата начала генерации (будет использован 00:00 этой даты).</param>
        public static void GenerateWeeklySchedule(DateTime startDate)
        {
            // Определяем границы недели
            DateTime weekStart = startDate.Date;
            DateTime weekEnd = weekStart.AddDays(1);

            using (var session = NHibernateHelper.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    // Получаем все сериалы, у которых уже есть хотя бы один эпизод
                    var allSerials = session.Query<Serial>().Where(s => s.EpisodesDb.Count >=1).ToList();
                    //var allSerials = session.Query<Serial>().ToList();
                    if (allSerials.Count == 0)
                    {
                        transaction.Commit();
                        return;
                    }

                    // Итерация по дням недели
                    DateTime currentDay = weekStart;
                    while (currentDay < weekEnd)
                    {
                        DateTime dayStart = currentDay.Date;
                        DateTime dayEnd = dayStart.AddDays(1);
                        DateTime currentTime = dayStart;

                        // Список сериалов, для которых уже добавлена серия в этот день
                        HashSet<int> usedSerialIdsForDay = new HashSet<int>();

                        // Заполняем день до его конца
                        while (currentTime < dayEnd)
                        {
                            Serial chosenSerial = null;
                            // Сначала выбираем сериал, для которого ещё не добавляли серию в этот день
                            var notUsedSerials = allSerials.Where(s => !usedSerialIdsForDay.Contains(s.Id) && s.EpisodesDb.Where(x=> x.State != State.Scheduled).Count()>0 && s.State == State.Viewing).ToList();
                            if (notUsedSerials.Any())
                            {
                                //int idx = new Random().Next(notUsedSerials.Count);
                                int idx = 0;
                                chosenSerial = notUsedSerials[idx];
                                //if (chosenSerial.EpisodesDb.Where(x => x.State != State.Scheduled).Count() == 0)
                                //{
                                //    //transaction.Commit();
                                //    break;
                                //}
                            }
                            else
                            {
                                notUsedSerials = allSerials.Where(s => !usedSerialIdsForDay.Contains(s.Id) && s.EpisodesDb.Where(x => x.State != State.Scheduled).Count() > 0 && s.State == State.Created && s.OneSeason == true).ToList();
                                if (notUsedSerials.Any())
                                {
                                    int idx = new Random().Next(notUsedSerials.Count);
                                    chosenSerial = notUsedSerials[idx];
                                    chosenSerial.State = State.Viewing;
                                    session.Update(chosenSerial);
                                }
                                else
                                {
                                    chosenSerial = allSerials[new Random().Next(allSerials.Count)];
                                }
                            }
                            
                            // Выбираем следующий эпизод для выбранного сериала, где State != \"Scheduled\"
                            var nextEpisode = session.Query<Episode>()
                                .Where(e => e.Serial.Id == chosenSerial.Id && e.State != State.Scheduled) // если State может быть null
                                .OrderBy(e => e.Id)
                                .FirstOrDefault();

                            // Если эпизодов для данного сериала больше нет, пропускаем его
                            if (nextEpisode == null)
                            {
                                chosenSerial.State = State.Viewed;
                                session.Update(chosenSerial);
                                if (chosenSerial.NextSeason == null)
                                {
                                    nextEpisode = session.Query<Episode>()
                                    .Where(e => e.State != State.Scheduled) // если State может быть null
                                    .OrderBy(e => e.Id)
                                    .FirstOrDefault();
                                }
                                else
                                {
                                    nextEpisode = session.Query<Episode>()
                                        .Where(e => e.Serial.Id == chosenSerial.NextSeason.Id && e.State != State.Scheduled) // если State может быть null
                                        .OrderBy(e => e.Id)
                                        .FirstOrDefault();
                                    if (nextEpisode != null)
                                    {
                                        chosenSerial.NextSeason.State = State.Viewing;
                                        session.Update(chosenSerial.NextSeason);
                                    }
                                }
                                // Если эпизодов вообще больше нету, заканчиваем генерацию
                                if (nextEpisode == null)
                                {
                                    break;
                                }
                                continue;
                            }

                            // Проверяем, помещается ли эпизод в оставшееся время дня
                            if (currentTime.Add(nextEpisode.Duration) <= dayEnd)
                            {
                                var schedule = new Schedule
                                {
                                    Start = currentTime,
                                    Episode = nextEpisode
                                };
                                session.Save(schedule);

                                // Отмечаем эпизод как запланированный
                                nextEpisode.State = State.Scheduled;
                                session.Update(nextEpisode);
                                
                                // Обновляем время на начало следующей трансляции
                                currentTime = currentTime.Add(nextEpisode.Duration);
                                // Отмечаем, что сегодня для этого сериала уже была показана серия
                                usedSerialIdsForDay.Add(chosenSerial.Id);
                            }
                            else
                            {
                                // Если эпизод не помещается, завершаем заполнение дня
                                break;
                            }
                        }
                        currentDay = currentDay.AddDays(1);
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
