--
-- PostgreSQL database dump
--

\restrict 3hrml2h0gvrqyfcUyFOVuv9dSpXA0MO60AJ2BBpazOaOOod1jChQgsXJCEMS13i

-- Dumped from database version 17.4
-- Dumped by pg_dump version 18.1

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: Users; Type: TABLE DATA; Schema: usr; Owner: postgres
--

INSERT INTO usr."Users" VALUES (1, NULL, NULL, 'da2dd5f9', 'admin', 'ADMIN', 'admin@site.com', 'ADMIN@SITE.COM', true, 'AQAAAAIAAYagAAAAEMTfZ+Yqc4ixTYXmPEFb7OnR9P0MVAsGI+Qz7J+Hj0I3oNmlbhnlbyXU6s7sLnpW9Q==', 'KGXQ5BYJVSXNK6P7L7T7I2QFH6KKQTP7', 'cdef89e2-7e32-42e9-b291-17a50f8664c0', NULL, true, false, NULL, false, 0, 'bear', 0, 0, 1, NULL, NULL, false);
INSERT INTO usr."Users" VALUES (2, 'Si Ni', 'Lim', '9aef1a10', 'Nini', 'NINI', 'limsini000@gmail.com', 'LIMSINI000@GMAIL.COM', true, 'AQAAAAIAAYagAAAAEEFvtaJFOZojMgskPy8AVBiFyy1BffcOFZVzt10z4SbkyxHL5uNLLa0OBliZFlfWhg==', '5HUYIZAGK2EOXGGBWQLD3THMWDR5UG3A', 'f80745b2-f2b9-4c69-9472-09a3c61d500d', '0123456789', true, false, NULL, false, 0, 'bear', 500, 350, 1, NULL, NULL, false);


--
-- Data for Name: ActivityLogs; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Badges; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Badges" VALUES (1, 'First Flame', 'Complete your first daily check-in.', 'badge_first_flame', 'common', 'streak', 1, '{"type":"streak","count":1}', true, '2026-03-08 16:28:01.030853+08', '2026-03-08 16:28:01.030853+08');
INSERT INTO public."Badges" VALUES (2, 'Week Warrior', 'Maintain a 7-day streak.', 'badge_week_warrior', 'rare', 'streak', 7, '{"type":"streak","count":7}', true, '2026-03-08 16:28:01.030852+08', '2026-03-08 16:28:01.030852+08');
INSERT INTO public."Badges" VALUES (3, 'Month Master', 'Maintain a 30-day streak.', 'badge_month_master', 'legendary', 'streak', 30, '{"type":"streak","count":30}', true, '2026-03-08 16:28:01.030852+08', '2026-03-08 16:28:01.030852+08');
INSERT INTO public."Badges" VALUES (4, 'Quiz Starter', 'Complete your first quiz.', 'badge_quiz_starter', 'common', 'quiz', 1, '{"type":"quizzes_completed","count":1}', true, '2026-03-08 16:28:01.030852+08', '2026-03-08 16:28:01.030852+08');
INSERT INTO public."Badges" VALUES (5, 'Quiz Enthusiast', 'Complete 10 quizzes.', 'badge_quiz_enthusiast', 'rare', 'quiz', 10, '{"type":"quizzes_completed","count":10}', true, '2026-03-08 16:28:01.030852+08', '2026-03-08 16:28:01.030852+08');
INSERT INTO public."Badges" VALUES (6, 'Quiz Master', 'Complete 50 quizzes.', 'badge_quiz_master', 'epic', 'quiz', 50, '{"type":"quizzes_completed","count":50}', true, '2026-03-08 16:28:01.030852+08', '2026-03-08 16:28:01.030852+08');
INSERT INTO public."Badges" VALUES (7, 'Level Up!', 'Reach level 2.', 'badge_level_up', 'common', 'level', 2, '{"type":"level","value":2}', true, '2026-03-08 16:28:01.030852+08', '2026-03-08 16:28:01.030852+08');
INSERT INTO public."Badges" VALUES (8, 'Rising Star', 'Reach level 5.', 'badge_rising_star', 'rare', 'level', 5, '{"type":"level","value":5}', true, '2026-03-08 16:28:01.030851+08', '2026-03-08 16:28:01.030851+08');
INSERT INTO public."Badges" VALUES (9, 'Legendary Learner', 'Reach level 10.', 'badge_legendary_learner', 'legendary', 'level', 10, '{"type":"level","value":10}', true, '2026-03-08 16:28:01.030851+08', '2026-03-08 16:28:01.030851+08');
INSERT INTO public."Badges" VALUES (10, 'First Friend', 'Add your first friend.', 'badge_first_friend', 'common', 'social', 1, '{"type":"friends","count":1}', true, '2026-03-08 16:28:01.030829+08', '2026-03-08 16:28:01.030841+08');
INSERT INTO public."Badges" VALUES (11, 'Social Butterfly', 'Have 10 friends.', 'badge_social_butterfly', 'rare', 'social', 10, '{"type":"friends","count":10}', true, '2026-03-08 16:28:01.030853+08', '2026-03-08 16:28:01.030853+08');
INSERT INTO public."Badges" VALUES (12, 'Classroom Champion', 'Get the highest score in a classroom quiz.', 'badge_classroom_champion', 'epic', 'classroom', 1, '{"type":"classroom_top_score","count":1}', true, '2026-03-08 16:28:01.030853+08', '2026-03-08 16:28:01.030853+08');
INSERT INTO public."Badges" VALUES (13, 'Mission Accomplished', 'Complete your first special mission.', 'badge_mission_accomplished', 'rare', 'mission', 1, '{"type":"missions_completed","count":1}', true, '2026-03-08 16:28:01.030853+08', '2026-03-08 16:28:01.030853+08');


--
-- Data for Name: Classrooms; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ClassroomQuizzes; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ClassroomStudents; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: DailyCheckIns; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."DailyCheckIns" VALUES (1, 2, '2026-03-08', '2026-03-08 18:48:39.438618+08', '2026-03-08 18:48:39.438682+08');
INSERT INTO public."DailyCheckIns" VALUES (2, 2, '2026-03-09', '2026-03-09 14:17:49.253665+08', '2026-03-09 14:17:49.25369+08');


--
-- Data for Name: ShopItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."ShopItems" VALUES (9, 'Pirate King', 'Arr! A swashbuckling pirate bear variant.', 'avatar', 800, 'diamonds', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FPirate.png?alt=media', 'common', NULL, true, false, NULL, '2026-03-09 19:21:57.891177+08', '2026-03-09 19:21:57.89122+08');
INSERT INTO public."ShopItems" VALUES (10, 'Crown', 'A regal bear variant fit for a quiz champion.', 'avatar', 1500, 'diamonds', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FKing.png?alt=media', 'rare', NULL, true, false, NULL, '2026-03-09 19:21:57.891244+08', '2026-03-09 19:21:57.891251+08');
INSERT INTO public."ShopItems" VALUES (11, 'Giyu', 'Demon Slayer', 'avatar', 1750, 'diamonds', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FGiyu.png?alt=media', 'rare', NULL, true, false, NULL, '2026-03-09 19:21:57.891177+08', '2026-03-09 19:21:57.89122+08');
INSERT INTO public."ShopItems" VALUES (12, 'Rengoku', 'Demon Slayer', 'avatar', 1850, 'diamonds', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FRengoku.png?alt=media', 'rare', NULL, true, false, NULL, '2026-03-09 19:21:57.891177+08', '2026-03-09 19:21:57.89122+08');
INSERT INTO public."ShopItems" VALUES (13, 'Inosuke', 'Demon Slayer', 'avatar', 1500, 'diamonds', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/mascots%2FInosuke.png?alt=media', 'rare', NULL, true, false, NULL, '2026-03-09 19:21:57.891177+08', '2026-03-09 19:21:57.89122+08');


--
-- Data for Name: DailySpecials; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: DiamondTransactions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."DiamondTransactions" VALUES (1, 2, -50, 'Purchased Pirate Hat', '1', '2026-03-09 15:12:49.769053+08', '2026-03-09 15:12:49.769067+08');
INSERT INTO public."DiamondTransactions" VALUES (2, 2, -1500, 'Purchased Inosuke', '13', '2026-03-09 22:45:32.780335+08', '2026-03-09 22:45:32.780356+08');


--
-- Data for Name: FeaturedBadges; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Friendships; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: GameCatalogs; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GameCatalogs" VALUES (1, 'magic_backpack', 'Magic Backpack', 'magic_backpack', '2026-03-08 16:28:00.865971+08', '2026-03-08 16:28:00.865999+08');
INSERT INTO public."GameCatalogs" VALUES (2, 'word_bridge', 'Word Bridge', 'word_bridge', '2026-03-08 16:28:00.866001+08', '2026-03-08 16:28:00.866001+08');
INSERT INTO public."GameCatalogs" VALUES (3, 'story_recall', 'Story Recall', 'story_recall', '2026-03-08 16:28:00.866001+08', '2026-03-08 16:28:00.866002+08');
INSERT INTO public."GameCatalogs" VALUES (4, 'picture_word_association', 'Picture Word Association', 'picture_word_association', '2026-03-08 16:28:00.866002+08', '2026-03-08 16:28:00.866002+08');
INSERT INTO public."GameCatalogs" VALUES (5, 'translation', 'Translation', 'translation', '2026-03-08 16:28:00.866002+08', '2026-03-08 16:28:00.866002+08');


--
-- Data for Name: GameConfigs; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GameConfigs" VALUES (1, 'magic_backpack', '2026-03-08 16:28:00.866002+08', '2026-03-08 16:28:00.866004+08', 'kindergarten', 'easy', 5, 1, 'school');
INSERT INTO public."GameConfigs" VALUES (2, 'word_bridge', '2026-03-08 16:28:00.866022+08', '2026-03-08 16:28:00.866022+08', 'primary', 'easy', 0, 1, '');


--
-- Data for Name: GameDifficulties; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GameDifficulties" VALUES (1, 1, 1, 'Easy', 3, 800, false, 'Short sequences', '2026-03-08 16:28:00.866006+08', '2026-03-08 16:28:00.866008+08');
INSERT INTO public."GameDifficulties" VALUES (2, 1, 2, 'Medium', 4, 650, false, 'Moderate sequences', '2026-03-08 16:28:00.866009+08', '2026-03-08 16:28:00.866009+08');
INSERT INTO public."GameDifficulties" VALUES (3, 1, 3, 'Hard', 5, 500, true, 'Faster with ghost mode', '2026-03-08 16:28:00.86601+08', '2026-03-08 16:28:00.86601+08');
INSERT INTO public."GameDifficulties" VALUES (4, 2, 1, 'Easy', 0, 0, false, 'Short words and hints', '2026-03-08 16:28:00.866022+08', '2026-03-08 16:28:00.866022+08');
INSERT INTO public."GameDifficulties" VALUES (5, 2, 2, 'Hard', 0, 0, false, 'Longer words and fewer hints', '2026-03-08 16:28:00.866022+08', '2026-03-08 16:28:00.866022+08');


--
-- Data for Name: GameThemes; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GameThemes" VALUES (1, 1, 'school', 'School', '🎒', 'School supplies and classroom items', '2026-03-08 16:28:00.86601+08', '2026-03-08 16:28:00.866011+08');
INSERT INTO public."GameThemes" VALUES (2, 1, 'camping', 'Camping', '🏕️', 'Camping and outdoor items', '2026-03-08 16:28:00.86602+08', '2026-03-08 16:28:00.86602+08');


--
-- Data for Name: GameThemeGradients; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GameThemeGradients" VALUES (1, 1, 0, '#6366F1', '2026-03-08 16:28:00.866013+08', '2026-03-08 16:28:00.866014+08');
INSERT INTO public."GameThemeGradients" VALUES (2, 1, 1, '#4F46E5', '2026-03-08 16:28:00.866016+08', '2026-03-08 16:28:00.866016+08');
INSERT INTO public."GameThemeGradients" VALUES (3, 2, 0, '#10B981', '2026-03-08 16:28:00.86602+08', '2026-03-08 16:28:00.86602+08');
INSERT INTO public."GameThemeGradients" VALUES (4, 2, 1, '#059669', '2026-03-08 16:28:00.866021+08', '2026-03-08 16:28:00.866021+08');


--
-- Data for Name: GameThemeItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GameThemeItems" VALUES (1, 1, 'pencil', 'Pencil', '✏️', '2026-03-08 16:28:00.866016+08', '2026-03-08 16:28:00.866018+08');
INSERT INTO public."GameThemeItems" VALUES (2, 1, 'ruler', 'Ruler', '📏', '2026-03-08 16:28:00.866019+08', '2026-03-08 16:28:00.866019+08');
INSERT INTO public."GameThemeItems" VALUES (3, 1, 'apple', 'Apple', '🍎', '2026-03-08 16:28:00.866019+08', '2026-03-08 16:28:00.866019+08');
INSERT INTO public."GameThemeItems" VALUES (4, 1, 'notebook', 'Notebook', '📒', '2026-03-08 16:28:00.86602+08', '2026-03-08 16:28:00.86602+08');
INSERT INTO public."GameThemeItems" VALUES (5, 1, 'eraser', 'Eraser', '🧽', '2026-03-08 16:28:00.86602+08', '2026-03-08 16:28:00.86602+08');
INSERT INTO public."GameThemeItems" VALUES (6, 2, 'tent', 'Tent', '⛺', '2026-03-08 16:28:00.866021+08', '2026-03-08 16:28:00.866021+08');
INSERT INTO public."GameThemeItems" VALUES (7, 2, 'fire', 'Campfire', '🔥', '2026-03-08 16:28:00.866021+08', '2026-03-08 16:28:00.866021+08');
INSERT INTO public."GameThemeItems" VALUES (8, 2, 'flashlight', 'Flashlight', '🔦', '2026-03-08 16:28:00.866021+08', '2026-03-08 16:28:00.866021+08');
INSERT INTO public."GameThemeItems" VALUES (9, 2, 'map', 'Map', '🗺️', '2026-03-08 16:28:00.866021+08', '2026-03-08 16:28:00.866021+08');
INSERT INTO public."GameThemeItems" VALUES (10, 2, 'water', 'Water', '💧', '2026-03-08 16:28:00.866021+08', '2026-03-08 16:28:00.866021+08');


--
-- Data for Name: LeaderboardEntries; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Levels; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Levels" VALUES (1, 1, 'Beginner', 0, 'magic_backpack', '2026-03-08 16:28:00.95838+08', '2026-03-08 16:28:00.958393+08');
INSERT INTO public."Levels" VALUES (2, 2, 'Explorer', 100, 'word_bridge', '2026-03-08 16:28:00.958397+08', '2026-03-08 16:28:00.958397+08');
INSERT INTO public."Levels" VALUES (3, 3, 'Adventurer', 300, 'story_recall', '2026-03-08 16:28:00.958397+08', '2026-03-08 16:28:00.958397+08');
INSERT INTO public."Levels" VALUES (4, 4, 'Scholar', 600, NULL, '2026-03-08 16:28:00.958397+08', '2026-03-08 16:28:00.958398+08');
INSERT INTO public."Levels" VALUES (5, 5, 'Champion', 1000, NULL, '2026-03-08 16:28:00.958398+08', '2026-03-08 16:28:00.958398+08');
INSERT INTO public."Levels" VALUES (6, 6, 'Master', 1500, NULL, '2026-03-08 16:28:00.958398+08', '2026-03-08 16:28:00.958398+08');
INSERT INTO public."Levels" VALUES (7, 7, 'Legend', 2200, NULL, '2026-03-08 16:28:00.958398+08', '2026-03-08 16:28:00.958398+08');
INSERT INTO public."Levels" VALUES (8, 8, 'Mythic', 3000, NULL, '2026-03-08 16:28:00.958398+08', '2026-03-08 16:28:00.958398+08');
INSERT INTO public."Levels" VALUES (9, 9, 'Celestial', 4000, NULL, '2026-03-08 16:28:00.958398+08', '2026-03-08 16:28:00.958398+08');
INSERT INTO public."Levels" VALUES (10, 10, 'Transcendent', 5500, NULL, '2026-03-08 16:28:00.958398+08', '2026-03-08 16:28:00.958398+08');


--
-- Data for Name: QuizAttempts; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."QuizAttempts" VALUES (1, 'bd72cfe4f3224812a6ce599b41f93320', 'word_bridge_001', 2, '2026-03-08 16:53:21.039379+08', NULL, NULL, '', '', '2026-03-08 16:53:21.06802+08', '2026-03-08 16:53:21.06807+08');
INSERT INTO public."QuizAttempts" VALUES (2, '1c789f09a6bb454ab6afb752261bed6f', 'word_bridge_001', 2, '2026-03-08 18:17:53.051518+08', NULL, NULL, '', '', '2026-03-08 18:17:53.077894+08', '2026-03-08 18:17:53.077929+08');
INSERT INTO public."QuizAttempts" VALUES (3, 'c7b7dfab8854450d8a06a93e5fbd5239', 'magic_backpack', 2, '2026-03-09 14:46:46.444695+08', NULL, NULL, '', '', '2026-03-09 14:46:46.51175+08', '2026-03-09 14:46:46.511777+08');


--
-- Data for Name: QuizAttemptAnswers; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: MagicBackpackAttemptAnswers; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: MagicBackpackAttemptSelections; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Quizzes; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Quizzes" VALUES (1, 'magic_backpack', 'Magic Backpack: Pack & Remember', 'Watch as items drop into the backpack, then select the items in the same order.', 'School', 'default', 'beginner', 6, 50, '2025-01-01T00:00:00Z', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fbackpack.png?alt=media', 'memory', '2026-03-08 16:28:00.86587+08', '2026-03-08 16:28:00.865902+08', 'magic_backpack');
INSERT INTO public."Quizzes" VALUES (2, 'word_bridge', 'Word Builder Bridge: Spell & Learn', 'Drag letters to build words! Practice spelling while crossing the bridge.', 'Spelling & Vocabulary', 'default', 'beginner', 10, 150, '2024-01-01T00:00:00Z', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fbuilder.png?alt=media', 'memory', '2026-03-08 16:28:00.865951+08', '2026-03-08 16:28:00.865953+08', 'word_bridge');
INSERT INTO public."Quizzes" VALUES (3, 'story_recall', 'Word Twins', 'Flip the cards and match the word with the image!', 'Story Comprehension Training', 'default', 'beginner', 15, 100, '2024-01-01T00:00:00Z', 'https://firebasestorage.googleapis.com/v0/b/vega-b7b3c.firebasestorage.app/o/thumbnails%2Fwordpair.png?alt=media', 'memory', '2026-03-08 16:28:00.865959+08', '2026-03-08 16:28:00.865959+08', 'story_recall');


--
-- Data for Name: QuizQuestions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."QuizQuestions" VALUES (1, 'mb_1', 'magic_backpack', 'Pack it right! Remember which items go into the backpack.', 50, 1, '2026-03-08 16:28:00.865923+08', '2026-03-08 16:28:00.865929+08', 'Pack items correctly to build vocabulary and memory strength!');
INSERT INTO public."QuizQuestions" VALUES (2, 'wb_1', 'word_bridge', '🌉 Spell the Animal: TIGER', 30, 2, '2026-03-08 16:28:00.865954+08', '2026-03-08 16:28:00.865954+08', 'Great spelling! Tigers are amazing big cats.');
INSERT INTO public."QuizQuestions" VALUES (3, 'wb_2', 'word_bridge', '🌉 Spell the Fruit: APPLE', 30, 2, '2026-03-08 16:28:00.865958+08', '2026-03-08 16:28:00.865958+08', 'Delicious! An apple a day keeps the doctor away.');
INSERT INTO public."QuizQuestions" VALUES (4, 'story1', 'story_recall', '📚 Story Time: The Little Bear''s Adventure', 50, 3, '2026-03-08 16:28:00.865959+08', '2026-03-08 16:28:00.865959+08', '');


--
-- Data for Name: MagicBackpackQuestions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."MagicBackpackQuestions" VALUES (1, 1, 'School', 'kindergarten', '2026-03-08 16:28:00.865932+08', '2026-03-08 16:28:00.865936+08');


--
-- Data for Name: MagicBackpackItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."MagicBackpackItems" VALUES (1, 1, 'pencil', 'Pencil', '✏️', '2026-03-08 16:28:00.865938+08', '2026-03-08 16:28:00.865941+08');
INSERT INTO public."MagicBackpackItems" VALUES (2, 1, 'ruler', 'Ruler', '📏', '2026-03-08 16:28:00.865942+08', '2026-03-08 16:28:00.865942+08');
INSERT INTO public."MagicBackpackItems" VALUES (3, 1, 'apple', 'Apple', '🍎', '2026-03-08 16:28:00.865942+08', '2026-03-08 16:28:00.865942+08');
INSERT INTO public."MagicBackpackItems" VALUES (4, 1, 'notebook', 'Notebook', '📒', '2026-03-08 16:28:00.865942+08', '2026-03-08 16:28:00.865942+08');
INSERT INTO public."MagicBackpackItems" VALUES (5, 1, 'eraser', 'Eraser', '🧽', '2026-03-08 16:28:00.865943+08', '2026-03-08 16:28:00.865943+08');


--
-- Data for Name: MagicBackpackSequences; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."MagicBackpackSequences" VALUES (1, 1, 'pencil', 0, '2026-03-08 16:28:00.865943+08', '2026-03-08 16:28:00.865949+08');
INSERT INTO public."MagicBackpackSequences" VALUES (2, 1, 'apple', 1, '2026-03-08 16:28:00.865951+08', '2026-03-08 16:28:00.865951+08');
INSERT INTO public."MagicBackpackSequences" VALUES (3, 1, 'ruler', 2, '2026-03-08 16:28:00.865951+08', '2026-03-08 16:28:00.865951+08');


--
-- Data for Name: Mascots; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Mascots" VALUES (1, 'Bear', 'bear', 'A friendly bear companion who loves learning!', true, NULL, '2026-03-08 16:28:00.991952+08', '2026-03-08 16:28:00.991952+08');
INSERT INTO public."Mascots" VALUES (2, 'Fox', 'fox', 'A clever fox who helps you solve puzzles.', false, '{"type":"level","value":3}', '2026-03-08 16:28:00.991952+08', '2026-03-08 16:28:00.991952+08');
INSERT INTO public."Mascots" VALUES (3, 'Owl', 'owl', 'A wise owl that guides you through stories.', false, '{"type":"level","value":5}', '2026-03-08 16:28:00.991952+08', '2026-03-08 16:28:00.991952+08');
INSERT INTO public."Mascots" VALUES (4, 'Rabbit', 'rabbit', 'A speedy rabbit that races through word challenges.', false, '{"type":"streak","value":7}', '2026-03-08 16:28:00.991951+08', '2026-03-08 16:28:00.991951+08');
INSERT INTO public."Mascots" VALUES (5, 'Dragon', 'dragon', 'A mighty dragon unlocked by true dedication.', false, '{"type":"level","value":10}', '2026-03-08 16:28:00.991934+08', '2026-03-08 16:28:00.991945+08');


--
-- Data for Name: SpecialMissions; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: StoryRecallAttemptAnswers; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: StoryRecallAttemptSelections; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: StoryRecallQuestions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."StoryRecallQuestions" VALUES (1, 4, 'bear_adventure', '/assets/audio/bear_adventure.mp3', 'A little bear went on an adventure...', '2026-03-08 16:28:00.865959+08', '2026-03-08 16:28:00.865961+08');


--
-- Data for Name: StoryRecallItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."StoryRecallItems" VALUES (1, 1, 'q1', 'Where did the bear go?', 0, '2026-03-08 16:28:00.865963+08', '2026-03-08 16:28:00.865965+08');


--
-- Data for Name: StoryRecallOptions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."StoryRecallOptions" VALUES (1, 1, 0, 'Forest', '2026-03-08 16:28:00.865967+08', '2026-03-08 16:28:00.865969+08');
INSERT INTO public."StoryRecallOptions" VALUES (2, 1, 1, 'Beach', '2026-03-08 16:28:00.865971+08', '2026-03-08 16:28:00.865971+08');
INSERT INTO public."StoryRecallOptions" VALUES (3, 1, 2, 'City', '2026-03-08 16:28:00.865971+08', '2026-03-08 16:28:00.865971+08');
INSERT INTO public."StoryRecallOptions" VALUES (4, 1, 3, 'Mountain', '2026-03-08 16:28:00.865971+08', '2026-03-08 16:28:00.865971+08');


--
-- Data for Name: UserBadges; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: UserEquippedItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."UserEquippedItems" VALUES (1, 2, 'avatar', 13, '2026-03-09 22:51:09.404201+08', '2026-03-09 22:51:09.40421+08');


--
-- Data for Name: UserInventoryItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."UserInventoryItems" VALUES (2, 2, 13, '2026-03-09 22:45:32.705486+08', '2026-03-09 22:45:32.751887+08', '2026-03-09 22:45:32.751909+08');


--
-- Data for Name: UserMascots; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: UserMissions; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: UserProgresses; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: UserStreaks; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."UserStreaks" VALUES (1, 2, 2, 7, '2026-03-09', '2026-02-28 09:15:21+08', '2026-03-09 14:17:49.318935+08');


--
-- Data for Name: WordBridgeAttemptAnswers; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: WordBridgeQuestions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."WordBridgeQuestions" VALUES (1, 2, 'TIGER', 'Harimau', 'easy', '', '2026-03-08 16:28:00.865955+08', '2026-03-08 16:28:00.865957+08');
INSERT INTO public."WordBridgeQuestions" VALUES (2, 3, 'APPLE', 'Epal', 'easy', '', '2026-03-08 16:28:00.865959+08', '2026-03-08 16:28:00.865959+08');


--
-- Data for Name: WordLists; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Words; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."__EFMigrationsHistory" VALUES ('20250505073716_initialcreate', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20250505142256_AddTables', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260205103800_AddUserProfileFields', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260205113907_UpdatePasswordResetTokenHash', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260205130939_AddQuizPersistence', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260205132610_NormalizeQuizSchema', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260307124711_AddGameFeatures', '8.0.10');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260308082721_AddGameTypeToQuiz', '8.0.10');


--
-- Data for Name: Roles; Type: TABLE DATA; Schema: usr; Owner: postgres
--

INSERT INTO usr."Roles" VALUES (1, 'Administrator', '2026-03-08 16:28:00.221012+08', 'admin', 'ADMIN', NULL);
INSERT INTO usr."Roles" VALUES (2, 'Student', '2026-03-08 16:28:00.377888+08', 'student', 'STUDENT', NULL);
INSERT INTO usr."Roles" VALUES (3, 'Teacher', '2026-03-08 16:28:00.39891+08', 'teacher', 'TEACHER', NULL);


--
-- Data for Name: RoleClaims; Type: TABLE DATA; Schema: usr; Owner: postgres
--



--
-- Data for Name: UserClaims; Type: TABLE DATA; Schema: usr; Owner: postgres
--



--
-- Data for Name: UserLogins; Type: TABLE DATA; Schema: usr; Owner: postgres
--



--
-- Data for Name: UserRefreshTokens; Type: TABLE DATA; Schema: usr; Owner: postgres
--

INSERT INTO usr."UserRefreshTokens" VALUES ('4a82e768-8a29-44e7-875a-16f1499c2c09', 2, '2026-03-08 16:35:22.471516+08', true, '2026-03-08 16:35:22.514505+08', '2026-03-08 16:35:22.514544+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('cceb49d3-ce7f-4714-85e9-804cc9da9ade', 2, '2026-03-08 16:36:12.03204+08', true, '2026-03-08 16:36:12.033106+08', '2026-03-08 16:36:12.033116+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('d32e0302-1142-47e2-a2e9-fba4a78fc57a', 2, '2026-03-08 16:36:38.315378+08', true, '2026-03-08 16:36:38.315832+08', '2026-03-08 16:36:38.315833+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('6f567dc9-7573-4194-a99c-02287fdff430', 2, '2026-03-08 16:41:50.124577+08', true, '2026-03-08 16:41:50.12508+08', '2026-03-08 16:41:50.12508+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('4a245b24-a586-4c59-a9c0-28e9a61188b9', 2, '2026-03-08 16:46:36.298728+08', true, '2026-03-08 16:46:36.360953+08', '2026-03-08 16:46:36.36098+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('e849d80b-d7f7-4d5b-b8da-363ea6eec563', 2, '2026-03-08 16:52:04.232802+08', true, '2026-03-08 16:52:04.30419+08', '2026-03-08 16:52:04.30422+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('8885c60f-08f8-4d3d-8bf6-cd7d2108cb9d', 2, '2026-03-08 16:55:49.943799+08', true, '2026-03-08 16:55:50.012286+08', '2026-03-08 16:55:50.012317+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('8f501b7b-ba3a-465a-82c2-9007115969a9', 2, '2026-03-08 17:27:18.305575+08', true, '2026-03-08 17:27:18.364434+08', '2026-03-08 17:27:18.364461+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('fa278385-889d-46fd-87d2-9185a1b5908e', 1, '2026-03-08 17:33:14.687686+08', true, '2026-03-08 17:33:14.737606+08', '2026-03-08 17:33:14.73763+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('3fddf463-bb7c-4b93-8170-0ab5097811d7', 1, '2026-03-08 17:33:32.09741+08', true, '2026-03-08 17:33:32.098462+08', '2026-03-08 17:33:32.098476+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('19058ffa-888a-431f-838f-1f33f3b1b19a', 2, '2026-03-08 17:34:16.497269+08', true, '2026-03-08 17:34:16.498649+08', '2026-03-08 17:34:16.498649+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('9598e870-8b57-4af4-9645-73f612512a2a', 1, '2026-03-08 17:34:57.716246+08', true, '2026-03-08 17:34:57.716544+08', '2026-03-08 17:34:57.716544+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('4b27ae56-11b5-45c9-8766-b8f1709e9e62', 2, '2026-03-08 18:09:56.189909+08', true, '2026-03-08 18:09:56.256117+08', '2026-03-08 18:09:56.256155+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('1755ba93-f875-44bb-826b-a8eaa2162beb', 1, '2026-03-08 18:12:52.010866+08', true, '2026-03-08 18:12:52.01183+08', '2026-03-08 18:12:52.011835+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('29a1b16d-cfe6-42b2-a61a-319ac18ef407', 2, '2026-03-08 18:14:31.486493+08', true, '2026-03-08 18:14:31.487342+08', '2026-03-08 18:14:31.487342+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('7378dced-d7bc-484b-a533-aaf3f2e088d6', 1, '2026-03-08 18:21:51.80139+08', true, '2026-03-08 18:21:51.853079+08', '2026-03-08 18:21:51.8531+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('c7afdde8-3179-4f2b-a3fa-9ba31a3604fe', 2, '2026-03-08 18:22:33.960123+08', true, '2026-03-08 18:22:33.962567+08', '2026-03-08 18:22:33.96258+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('027649ed-b793-4e52-9591-b61c53092b99', 2, '2026-03-09 15:20:20.373732+08', true, '2026-03-09 15:20:20.375096+08', '2026-03-09 15:20:20.375101+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('a3f637b3-6d8b-4e2d-a329-1bcd0d92cb2a', 2, '2026-03-09 14:32:39.679385+08', false, '2026-03-09 14:32:39.750425+08', '2026-03-09 19:31:44.277696+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('a1f35ad9-1512-4d57-84fa-dfac1bc8612a', 2, '2026-03-09 19:31:44.432197+08', true, '2026-03-09 19:31:44.442858+08', '2026-03-09 19:31:44.442908+08');
INSERT INTO usr."UserRefreshTokens" VALUES ('e951127f-eb30-4cbe-811c-c5f9bb031fcf', 2, '2026-03-09 19:54:47.005218+08', true, '2026-03-09 19:54:47.057803+08', '2026-03-09 19:54:47.05781+08');


--
-- Data for Name: UserRoles; Type: TABLE DATA; Schema: usr; Owner: postgres
--

INSERT INTO usr."UserRoles" VALUES (1, 1, '2026-03-08 16:28:00.585979+08');
INSERT INTO usr."UserRoles" VALUES (2, 2, '2026-03-08 16:35:04.867137+08');


--
-- Data for Name: UserTokens; Type: TABLE DATA; Schema: usr; Owner: postgres
--



--
-- Name: ActivityLogs_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ActivityLogs_Id_seq"', 1, false);


--
-- Name: Badges_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Badges_Id_seq"', 13, true);


--
-- Name: ClassroomQuizzes_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ClassroomQuizzes_Id_seq"', 1, false);


--
-- Name: ClassroomStudents_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ClassroomStudents_Id_seq"', 1, false);


--
-- Name: Classrooms_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Classrooms_Id_seq"', 1, false);


--
-- Name: DailyCheckIns_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."DailyCheckIns_Id_seq"', 2, true);


--
-- Name: DailySpecials_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."DailySpecials_Id_seq"', 1, false);


--
-- Name: DiamondTransactions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."DiamondTransactions_Id_seq"', 2, true);


--
-- Name: FeaturedBadges_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."FeaturedBadges_Id_seq"', 1, false);


--
-- Name: Friendships_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Friendships_Id_seq"', 1, false);


--
-- Name: GameCatalogs_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."GameCatalogs_Id_seq"', 5, true);


--
-- Name: GameConfigs_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."GameConfigs_Id_seq"', 2, true);


--
-- Name: GameDifficulties_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."GameDifficulties_Id_seq"', 5, true);


--
-- Name: GameThemeGradients_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."GameThemeGradients_Id_seq"', 4, true);


--
-- Name: GameThemeItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."GameThemeItems_Id_seq"', 10, true);


--
-- Name: GameThemes_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."GameThemes_Id_seq"', 2, true);


--
-- Name: LeaderboardEntries_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."LeaderboardEntries_Id_seq"', 1, false);


--
-- Name: Levels_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Levels_Id_seq"', 10, true);


--
-- Name: MagicBackpackAttemptAnswers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MagicBackpackAttemptAnswers_Id_seq"', 1, false);


--
-- Name: MagicBackpackAttemptSelections_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MagicBackpackAttemptSelections_Id_seq"', 1, false);


--
-- Name: MagicBackpackItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MagicBackpackItems_Id_seq"', 5, true);


--
-- Name: MagicBackpackQuestions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MagicBackpackQuestions_Id_seq"', 1, true);


--
-- Name: MagicBackpackSequences_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MagicBackpackSequences_Id_seq"', 3, true);


--
-- Name: Mascots_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Mascots_Id_seq"', 5, true);


--
-- Name: QuizAttemptAnswers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."QuizAttemptAnswers_Id_seq"', 1, false);


--
-- Name: QuizAttempts_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."QuizAttempts_Id_seq"', 3, true);


--
-- Name: QuizQuestions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."QuizQuestions_Id_seq"', 4, true);


--
-- Name: Quizzes_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Quizzes_Id_seq"', 3, true);


--
-- Name: ShopItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ShopItems_Id_seq"', 16, true);


--
-- Name: SpecialMissions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."SpecialMissions_Id_seq"', 1, false);


--
-- Name: StoryRecallAttemptAnswers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."StoryRecallAttemptAnswers_Id_seq"', 1, false);


--
-- Name: StoryRecallAttemptSelections_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."StoryRecallAttemptSelections_Id_seq"', 1, false);


--
-- Name: StoryRecallItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."StoryRecallItems_Id_seq"', 1, true);


--
-- Name: StoryRecallOptions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."StoryRecallOptions_Id_seq"', 4, true);


--
-- Name: StoryRecallQuestions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."StoryRecallQuestions_Id_seq"', 1, true);


--
-- Name: UserBadges_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserBadges_Id_seq"', 1, false);


--
-- Name: UserEquippedItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserEquippedItems_Id_seq"', 1, true);


--
-- Name: UserInventoryItems_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserInventoryItems_Id_seq"', 2, true);


--
-- Name: UserMascots_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserMascots_Id_seq"', 1, false);


--
-- Name: UserMissions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserMissions_Id_seq"', 1, false);


--
-- Name: UserProgresses_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserProgresses_Id_seq"', 1, false);


--
-- Name: UserStreaks_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."UserStreaks_Id_seq"', 1, false);


--
-- Name: WordBridgeAttemptAnswers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."WordBridgeAttemptAnswers_Id_seq"', 1, false);


--
-- Name: WordBridgeQuestions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."WordBridgeQuestions_Id_seq"', 2, true);


--
-- Name: WordLists_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."WordLists_Id_seq"', 1, false);


--
-- Name: Words_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Words_Id_seq"', 1, false);


--
-- Name: RoleClaims_Id_seq; Type: SEQUENCE SET; Schema: usr; Owner: postgres
--

SELECT pg_catalog.setval('usr."RoleClaims_Id_seq"', 1, false);


--
-- Name: Roles_Id_seq; Type: SEQUENCE SET; Schema: usr; Owner: postgres
--

SELECT pg_catalog.setval('usr."Roles_Id_seq"', 3, true);


--
-- Name: UserClaims_Id_seq; Type: SEQUENCE SET; Schema: usr; Owner: postgres
--

SELECT pg_catalog.setval('usr."UserClaims_Id_seq"', 1, false);


--
-- Name: Users_UserId_seq; Type: SEQUENCE SET; Schema: usr; Owner: postgres
--

SELECT pg_catalog.setval('usr."Users_UserId_seq"', 2, true);


--
-- PostgreSQL database dump complete
--

\unrestrict 3hrml2h0gvrqyfcUyFOVuv9dSpXA0MO60AJ2BBpazOaOOod1jChQgsXJCEMS13i

